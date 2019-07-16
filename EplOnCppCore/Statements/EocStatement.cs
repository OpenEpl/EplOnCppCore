using QIQI.EProjectFile.Statements;
using System;

namespace QIQI.EplOnCpp.Core.Statements
{
    public abstract class EocStatement
    {
        public CodeConverter C { get; }
        public ProjectConverter P => C.P;
        public ILoggerWithContext Logger => P.Logger;

        public EocStatement(CodeConverter c)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
        }

        public abstract void WriteTo(CodeWriter writer);
        public virtual EocStatement Optimize() => this;

        public static EocStatement Translate(CodeConverter converter, Statement item)
        {
            switch (item)
            {
                case null:
                    return null;

                case ExpressionStatement v:
                    return EocExpressionStatement.Translate(converter, v);

                case UnexaminedStatement v:
                    throw new Exception("不允许出现未验证语句：" + v.UnexaminedCode);
                case IfElseStatement v:
                    return EocIfElseStatement.Translate(converter, v);

                case IfStatement v:
                    return EocIfStatement.Translate(converter, v);

                case WhileStatement v:
                    return EocWhileStatement.Translate(converter, v);

                case DoWhileStatement v:
                    return EocDoWhileStatement.Translate(converter, v);

                case CounterStatement v:
                    return EocCounterStatement.Translate(converter, v);

                case ForStatement v:
                    return EocForStatement.Translate(converter, v);

                case SwitchStatement v:
                    return EocSwitchStatement.Translate(converter, v);

                default:
                    throw new Exception("<error-statement>");
            }
        }
    }
}