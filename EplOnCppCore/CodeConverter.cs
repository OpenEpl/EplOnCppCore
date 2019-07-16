using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EplOnCpp.Core.Statements;
using QIQI.EProjectFile;
using QIQI.EProjectFile.Statements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class CodeConverter
    {
        public ProjectConverter P { get; }
        public CodeWriter Writer { get; }
        public ClassInfo ClassItem { get; }
        public MethodInfo MethodItem { get; }
        public ILoggerWithContext Logger => P.Logger;
        public MethodParameterInfo[] Parameters { get; }
        public Dictionary<int, MethodParameterInfo> ParamIdMap { get; }
        public Dictionary<int, LocalVariableInfo> LocalIdMap { get; }
        public EocStatementBlock StatementBlock { get; set; }

        public CodeConverter(ProjectConverter projectConverter, CodeWriter writer, ClassInfo classItem, MethodInfo methodItem)
        {
            this.P = projectConverter;
            this.Writer = writer;
            this.ClassItem = classItem;
            this.MethodItem = methodItem;
            this.Parameters = methodItem.Parameters;
            this.ParamIdMap = methodItem.Parameters.ToDictionary(x => x.Id);
            this.LocalIdMap = methodItem.Variables.ToDictionary(x => x.Id);
            this.StatementBlock = EocStatementBlock.Translate(this, CodeDataParser.ParseStatementBlock(methodItem.CodeData.ExpressionData, methodItem.CodeData.Encoding));
        }

        private int TempVarId = 0;

        public string AllocTempVar()
        {
            return $"eoc_temp{TempVarId++}";
        }

        public CodeConverter Optimize()
        {
            StatementBlock = StatementBlock.Optimize();
            return this;
        }

        public void Generate()
        {
            foreach (var x in Parameters)
            {
                if (x.OptionalParameter)
                {
                    var name = P.GetUserDefinedName_SimpleCppName(x.Id);
                    var realValueType = P.GetCppTypeName(x.DataType, x.ArrayParameter);
                    var nullParameter = P.GetNullParameter(x.DataType, x.ArrayParameter);
                    var initValue = P.GetInitValue(x.DataType, x.ArrayParameter);
                    Writer.NewLine();
                    Writer.Write($"bool eoc_isNull_{name} = !{name}.has_value();");
                    if (x.ByRef || x.ArrayParameter || !P.IsValueType(x.DataType))
                    {
                        Writer.NewLine();
                        if (string.IsNullOrWhiteSpace(nullParameter))
                        {
                            Writer.Write($"{realValueType} eoc_default_{name};");
                        }
                        else
                        {
                            Writer.Write($"{realValueType} eoc_default_{name}({nullParameter});");
                        }

                        Writer.NewLine();
                        Writer.Write($"{realValueType}& eoc_value_{name} = eoc_isNull_{name} ? (eoc_default_{name} = {initValue}) : {name}.value().get();");
                    }
                    else
                    {
                        Writer.NewLine();
                        Writer.Write($"{realValueType} eoc_value_{name} = eoc_isNull_{name} ? {initValue} : {name}.value();");
                    }
                }
            }
            StatementBlock.WriteTo();
        }

        public void AddCommentLine(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                Writer.NewLine();
                Writer.Write("// ");
                Writer.Write(comment);
            }
        }

        public void AddComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                Writer.Write("// ");
                Writer.Write(comment);
            }
        }

        public void WriteLetExpression(EocExpression target, Action writeValue)
        {
            if (target is EocAccessMemberExpression expr && expr.MemberInfo.Setter != null)
            {
                expr.Target.WriteTo();
                Writer.Write("->");
                Writer.Write(expr.MemberInfo.Setter);
                Writer.Write("(");
                writeValue();
                Writer.Write(")");
                return;
            }
            target.WriteTo();
            Writer.Write(" = ");
            writeValue();
        }
    }
}