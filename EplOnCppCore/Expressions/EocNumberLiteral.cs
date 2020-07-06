using QIQI.EProjectFile.Expressions;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocNumberLiteral : EocExpression
    {
        public static EocNumberLiteral Translate(CodeConverter C, NumberLiteral expr)
        {
            if (expr == null) return null;
            return new EocNumberLiteral(C, expr.Value);
        }

        public EocNumberLiteral(CodeConverter c, double value) : base(c)
        {
            Value = value;
        }

        public double Value { get; }

        public override bool TryGetConstValue(out object value)
        {
            double v = Value;
            if ((int)v == v)
            {
                value = (int)v;
            }
            else if ((long)v == v)
            {
                value = (long)v;
            }
            else
            {
                value = v;
            }
            return true;
        }

        public override CppTypeName GetResultType()
        {
            if (!TryGetConstValue(out var v))
                throw new Exception();
            return EocDataTypes.GetConstValueType(v);
        }

        public override void WriteTo(CodeWriter writer)
        {
            if (!TryGetConstValue(out var v))
                throw new Exception();
            writer.WriteLiteral(v);
        }
    }
}