using QIQI.EProjectFile.Expressions;

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

        public override CppTypeName GetResultType()
        {
            double v = Value;
            if ((int)v == v)
            {
                return ProjectConverter.CppTypeName_Int;
            }
            else if ((long)v == v)
            {
                return ProjectConverter.CppTypeName_Long;
            }
            else
            {
                return ProjectConverter.CppTypeName_Double;
            }
        }

        public override void WriteTo()
        {
            double v = Value;
            if ((int)v == v)
            {
                Writer.WriteLiteral((int)v);
            }
            else if ((long)v == v)
            {
                Writer.WriteLiteral((long)v);
            }
            else
            {
                Writer.WriteLiteral(v);
            }
        }
    }
}