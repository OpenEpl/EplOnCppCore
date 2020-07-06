using QIQI.EProjectFile.Expressions;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocStringLiteral : EocExpression
    {
        public static EocStringLiteral Translate(CodeConverter C, StringLiteral expr)
        {
            if (expr == null) return null;
            return new EocStringLiteral(C, expr.Value);
        }

        public EocStringLiteral(CodeConverter c, string value) : base(c)
        {
            Value = value;
        }

        public string Value { get; }

        public override CppTypeName GetResultType()
        {
            return EocDataTypes.String;
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