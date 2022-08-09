using QIQI.EProjectFile.Expressions;
using QuikGraph;
using System;
using System.Collections.Generic;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public abstract class EocExpression
    {
        public CodeConverter C { get; }
        public ProjectConverter P => C.P;
        public ILoggerWithContext Logger => P.Logger;

        public EocExpression(CodeConverter c)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
        }

        public abstract void WriteTo(CodeWriter writer);

        public virtual void WriteToWithCast(CodeWriter writer, CppTypeName cast)
        {
            var exprType = GetResultType();
            if (cast == null || cast == EocDataTypes.Auto || cast == exprType)
            {
                WriteTo(writer);
                return;
            }
            writer.Write("e::system::cast<");
            writer.Write(cast.ToString());
            writer.Write(">(");
            WriteTo(writer);
            writer.Write(")");
        }

        public abstract CppTypeName GetResultType();

        public virtual bool TryGetConstValue(out object value)
        {
            value = null;
            return false;
        }

        public virtual void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            ProcessSubExpression(x => x.AnalyzeDependencies(graph), false);
        }

        public virtual bool TryGetConstValueWithCast(CppTypeName cast, out object value)
        {
            if (!TryGetConstValue(out value))
                return false;
            var type = EocDataTypes.GetConstValueType(value);
            if (type == cast)
                return true;
            if (EocDataTypes.IsArithmeticType(type) && EocDataTypes.IsArithmeticType(cast)) 
            {
                var NumberConverter = new Dictionary<CppTypeName, Func<object, object>> {
                    { EocDataTypes.Byte, x => Convert.ToByte(x) },
                    { EocDataTypes.Short, x => Convert.ToInt16(x) },
                    { EocDataTypes.Int, x => Convert.ToInt32(x) },
                    { EocDataTypes.Long, x => Convert.ToInt64(x) },
                    { EocDataTypes.Float, x => Convert.ToSingle(x) },
                    { EocDataTypes.Double, x => Convert.ToDouble(x) },
                };
                if (NumberConverter.TryGetValue(cast, out var converter)) 
                {
                    value = converter(value);
                    return true;
                }
            }
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