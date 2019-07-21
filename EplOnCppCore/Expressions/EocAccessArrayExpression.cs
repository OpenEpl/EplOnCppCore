using QIQI.EProjectFile.Expressions;
using System;
using System.Collections.Generic;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocAccessArrayExpression : EocExpression
    {
        public static EocAccessArrayExpression Translate(CodeConverter C, AccessArrayExpression expr)
        {
            if (expr == null) return null;
            return new EocAccessArrayExpression(C, EocExpression.Translate(C, expr.Target), EocExpression.Translate(C, expr.Index));
        }

        public EocAccessArrayExpression(CodeConverter c, EocExpression target, EocExpression index) : base(c)
        {
            Target = target;
            Index = index;
        }

        public EocExpression Target { get; set; }
        public EocExpression Index { get; set; }

        public override CppTypeName GetResultType()
        {
            if (Target is EocAccessArrayExpression t)
            {
                return t.GetResultType();
            }
            var arrayItemType = Target.GetResultType();
            switch (arrayItemType)
            {
                case CppTypeName x when x.Name == "e::system::array" && (x.PtrInfos == null || x.PtrInfos.Count == 0):
                    return x.TypeParam[0];

                case null:
                case CppTypeName sc when sc == ProjectConverter.CppTypeName_SkipCheck:
                case CppTypeName any when any == ProjectConverter.CppTypeName_Any:
                    return arrayItemType;

                case CppTypeName x when x == ProjectConverter.CppTypeName_Bin:
                    return ProjectConverter.CppTypeName_Byte;

                default:
                    throw new NotImplementedException();
            }
        }

        public override void WriteTo(CodeWriter writer)
        {
            EocExpression target = this;
            var indexs = new List<EocExpression>();
            while (target is EocAccessArrayExpression t)
            {
                indexs.Add(t.Index);
                target = t.Target;
            }
            indexs.Reverse();
            target.WriteTo(writer);
            writer.Write(".At(");
            for (int i = 0; i < indexs.Count; i++)
            {
                var item = indexs[i];
                if (i != 0)
                    writer.Write(", ");
                item.WriteTo(writer);
            }
            writer.Write(")");
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            if (Target != null)
            {
                if (deep)
                    Target.ProcessSubExpression(processor, deep);
                Target = processor(Target);
            }
            if (Index != null)
            {
                if (deep)
                    Index.ProcessSubExpression(processor, deep);
                Index = processor(Index);
            }
        }
    }
}