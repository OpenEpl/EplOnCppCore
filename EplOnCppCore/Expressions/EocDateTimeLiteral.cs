using QIQI.EProjectFile.Expressions;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocDateTimeLiteral : EocExpression
    {
        public static EocDateTimeLiteral Translate(CodeConverter C, DateTimeLiteral expr)
        {
            if (expr == null) return null;
            return new EocDateTimeLiteral(C, expr.Value);
        }

        public EocDateTimeLiteral(CodeConverter c, DateTime value) : base(c)
        {
            Value = value;
        }

        public DateTime Value { get; }

        public override CppTypeName GetResultType()
        {
            return ProjectConverter.CppTypeName_DateTime;
        }

        public override void WriteTo()
        {
            Writer.Write($"e::system::datetime({Value.ToOADate()}/*{Value.ToString("yyyyMMddTHHmmss")}*/)");
        }
    }
}