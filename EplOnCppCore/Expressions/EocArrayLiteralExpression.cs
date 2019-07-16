using QIQI.EProjectFile.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocArrayLiteralExpression : EocExpression
    {
        public EocArrayLiteralExpression(CodeConverter c, List<EocExpression> item) : base(c)
        {
            Item = item;
        }

        public List<EocExpression> Item { get; set; }

        public static EocArrayLiteralExpression Translate(CodeConverter C, ArrayLiteralExpression expr)
        {
            if (expr == null) return null;
            return new EocArrayLiteralExpression(C, expr.Select(x => EocExpression.Translate(C, x)).ToList());
        }

        public override CppTypeName GetResultType()
        {
            foreach (var item in Item)
            {
                var elemType = item.GetResultType();
                if (elemType != ProjectConverter.CppTypeName_SkipCheck)
                {
                    return new CppTypeName(false, "e::system::array", new[] { elemType });
                }
            }
            return ProjectConverter.CppTypeName_Bin;
        }

        public override void WriteTo()
        {
            WriteToWithCast(null);
        }

        public override void WriteToWithCast(CppTypeName cast)
        {
            if (cast == null || cast == ProjectConverter.CppTypeName_SkipCheck || cast == ProjectConverter.CppTypeName_Any)
            {
                cast = GetResultType();
            }
            var resultType = cast;
            CppTypeName elemType;
            if (cast == ProjectConverter.CppTypeName_Bin)
            {
                elemType = ProjectConverter.CppTypeName_Byte;
            }
            else if (cast.Name == "e::system::array")
            {
                elemType = resultType.TypeParam[0];
            }
            else
            {
                throw new Exception("无效数组类型");
            }
            Writer.Write(resultType.ToString());
            Writer.Write(" {");
            for (int i = 0; i < Item.Count; i++)
            {
                var item = Item[i];
                if (i != 0)
                    Writer.Write(", ");
                item.WriteToWithCast(elemType);
            }
            Writer.Write("}");
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            for (int i = 0; i < Item.Count; i++)
            {
                if(deep)
                    Item[i].ProcessSubExpression(processor);
                Item[i] = processor(Item[i]);
            }
        }
    }
}