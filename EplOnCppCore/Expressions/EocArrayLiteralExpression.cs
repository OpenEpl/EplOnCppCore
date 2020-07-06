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
                if (elemType != EocDataTypes.Auto)
                {
                    return new CppTypeName(false, "e::system::array", new[] { elemType });
                }
            }
            return EocDataTypes.Bin;
        }

        public override void WriteTo(CodeWriter writer)
        {
            WriteToWithCast(writer, null);
        }

        public override void WriteToWithCast(CodeWriter writer, CppTypeName cast)
        {
            if (cast == null || cast == EocDataTypes.Auto || cast == EocDataTypes.Any)
            {
                cast = GetResultType();
            }
            var resultType = cast;
            CppTypeName elemType;
            if (cast == EocDataTypes.Bin)
            {
                elemType = EocDataTypes.Byte;
            }
            else if (cast.Name == "e::system::array")
            {
                elemType = resultType.TypeParam[0];
            }
            else
            {
                throw new Exception("无效数组类型");
            }
            writer.Write(resultType.ToString());
            writer.Write(" {");
            for (int i = 0; i < Item.Count; i++)
            {
                var item = Item[i];
                if (i != 0)
                    writer.Write(", ");
                item.WriteToWithCast(writer, elemType);
            }
            writer.Write("}");
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

        public override bool TryGetConstValue(out object value)
        {
            return TryGetConstValueWithCast(GetResultType(), out value);
        }

        public override bool TryGetConstValueWithCast(CppTypeName cast, out object value)
        {
            var resultType = cast;
            CppTypeName elemType;
            if (cast == EocDataTypes.Bin)
            {
                elemType = EocDataTypes.Byte;
            }
            else if (cast.Name == "e::system::array")
            {
                elemType = resultType.TypeParam[0];
            }
            else
            {
                value = null;
                return false;
            }
            var values = new object[Item.Count];
            for (int i = 0; i < Item.Count; i++)
            {
                if (!Item[i].TryGetConstValueWithCast(elemType, out values[i]))
                {
                    value = null;
                    return false;
                }
            }
            value = values;
            return true;
        }
    }
}