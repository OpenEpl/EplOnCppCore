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
        public ClassInfo ClassItem { get; }
        public MethodInfo MethodItem { get; }
        public ILoggerWithContext Logger => P.Logger;
        public MethodParameterInfo[] Parameters { get; }
        public Dictionary<int, MethodParameterInfo> ParamIdMap { get; }
        public Dictionary<int, LocalVariableInfo> LocalIdMap { get; }
        public EocStatementBlock StatementBlock { get; set; }

        public CodeConverter(ProjectConverter projectConverter, ClassInfo classItem, MethodInfo methodItem)
        {
            this.P = projectConverter;
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

        public void Generate(CodeWriter writer)
        {
            foreach (var x in Parameters)
            {
                if (x.OptionalParameter)
                {
                    var name = P.GetUserDefinedName_SimpleCppName(x.Id);
                    var realValueType = P.GetCppTypeName(x.DataType, x.ArrayParameter);
                    var nullParameter = P.GetNullParameter(x.DataType, x.ArrayParameter);
                    var initValue = P.GetInitValue(x.DataType, x.ArrayParameter);
                    writer.NewLine();
                    writer.Write($"bool eoc_isNull_{name} = !{name}.has_value();");
                    if (x.ByRef || x.ArrayParameter || !P.IsValueType(x.DataType))
                    {
                        writer.NewLine();
                        if (string.IsNullOrWhiteSpace(nullParameter))
                        {
                            writer.Write($"{realValueType} eoc_default_{name};");
                        }
                        else
                        {
                            writer.Write($"{realValueType} eoc_default_{name}({nullParameter});");
                        }

                        writer.NewLine();
                        writer.Write($"{realValueType}& eoc_value_{name} = eoc_isNull_{name} ? (eoc_default_{name} = {initValue}) : {name}.value().get();");
                    }
                    else
                    {
                        writer.NewLine();
                        writer.Write($"{realValueType} eoc_value_{name} = eoc_isNull_{name} ? {initValue} : {name}.value();");
                    }
                }
            }
            StatementBlock.WriteTo(writer);
        }

        public void WriteLetExpression(CodeWriter writer, EocExpression target, Action writeValue)
        {
            if (target is EocAccessMemberExpression expr && expr.MemberInfo.Setter != null)
            {
                expr.Target.WriteTo(writer);
                writer.Write("->");
                writer.Write(expr.MemberInfo.Setter);
                writer.Write("(");
                writeValue();
                writer.Write(")");
                return;
            }
            target.WriteTo(writer);
            writer.Write(" = ");
            writeValue();
        }
    }
}