using QIQI.EProjectFile.Expressions;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public abstract class EocExpression
    {
        public CodeConverter C { get; }
        public ProjectConverter P => C.P;
        public CodeWriter Writer => C.Writer;
        public ILoggerWithContext Logger => P.Logger;

        public EocExpression(CodeConverter c)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
        }

        public abstract void WriteTo();

        public virtual void WriteToWithCast(CppTypeName cast)
        {
            var exprType = GetResultType();
            if (cast == null || cast == ProjectConverter.CppTypeName_SkipCheck || cast == exprType)
            {
                WriteTo();
                return;
            }
            Writer.Write("e::system::cast<");
            Writer.Write(cast.ToString());
            Writer.Write(">(");
            WriteTo();
            Writer.Write(")");
        }

        public abstract CppTypeName GetResultType();

        public virtual bool TryGetConstValue(out object value)
        {
            value = null;
            return false;
        }

        public static EocExpression Translate(CodeConverter C, Expression expr)
        {
            switch (expr)
            {
                case null:
                    return null;

                case MethodPtrExpression v:
                    return EocMethodPtrExpression.Translate(C, v);

                case CallExpression v:
                    return EocCallExpression.Translate(C, v);

                case StringLiteral v:
                    return EocStringLiteral.Translate(C, v);

                case NumberLiteral v:
                    return EocNumberLiteral.Translate(C, v);

                case DateTimeLiteral v:
                    return EocDateTimeLiteral.Translate(C, v);

                case BoolLiteral v:
                    return EocBoolLiteral.Translate(C, v);

                case VariableExpression v:
                    return EocVariableExpression.Translate(C, v);

                case AccessArrayExpression v:
                    return EocAccessArrayExpression.Translate(C, v);

                case AccessMemberExpression v:
                    return EocAccessMemberExpression.Translate(C, v);

                case ConstantExpression v:
                    return EocConstantExpression.Translate(C, v);

                case EmnuConstantExpression v:
                    return EocConstantExpression.Translate(C, v);

                case ArrayLiteralExpression v:
                    return EocArrayLiteralExpression.Translate(C, v);

                case DefaultValueExpression v:
                    return null;

                default:
                    throw new Exception("<error-expression>");
            }
        }
    }
}