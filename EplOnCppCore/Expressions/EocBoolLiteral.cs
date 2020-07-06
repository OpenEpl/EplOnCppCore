using QIQI.EProjectFile.Expressions;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocBoolLiteral : EocExpression
    {
        public static EocBoolLiteral Translate(CodeConverter C, BoolLiteral expr)
        {
            if (expr == null) return null;
            return new EocBoolLiteral(C, expr.Value);
        }

        public EocBoolLiteral(CodeConverter c, bool value) : base(c)
        {
            Value = value;
        }

        public bool Value { get; }

        public override CppTypeName GetResultType()
        {
            return EocDataTypes.Bool;
        }

        public override void WriteTo(CodeWriter writer)
        {
            writer.WriteLiteral(Value);
        }

        public override bool TryGetConstValue(out object value)
        {
            value = Value;
            return true;
        }
    }
}