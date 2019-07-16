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
        public virtual bool TryGetConstValueWithCast(CppTypeName cast, out object value)
        {
            if (!TryGetConstValue(out value))
                return false;
            var type = ProjectConverter.GetConstValueType(value);
            if (type == cast)
                return true; //FIXME 类型自动转换
            return false;
        }

        public void ProcessSubExpression(Action<EocExpression> processor, bool deep = true)
        {
            ProcessSubExpression(x =>
            {
                processor(x);
                return x;
            }, deep);
        }

        public virtual void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {

        }

        public virtual EocExpression Optimize()
        {
            ProcessSubExpression(x => x.Optimize(), false);
            return this;
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