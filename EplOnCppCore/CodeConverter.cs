using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EplOnCpp.Core.Statements;
using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class CodeConverter
    {
        public ProjectConverter P { get; }
        public string Name { get; }
        public string RefId { get; }
        public EocCmdInfo Info { get; }
        public bool IsClassMember { get; }
        public EocClass ClassItem { get; }
        public MethodInfo MethodItem { get; }
        public ILoggerWithContext Logger => P.Logger;
        public SortedDictionary<int, EocParameterInfo> ParamMap { get; }
        public SortedDictionary<int, EocLocalVariableInfo> LocalMap { get; }
        public EocStatementBlock StatementBlock { get; set; }

        private static EocCmdInfo InferEocCmdInfo(ProjectConverter P, MethodInfo rawInfo)
        {
            var name = P.GetUserDefinedName_SimpleCppName(rawInfo.Id);
            string cppName;
            if (EplSystemId.GetType(P.MethodIdToClassMap[rawInfo.Id].Id) == EplSystemId.Type_Class)
            {
                cppName = name;
            }
            else
            {
                cppName = $"{P.CmdNamespace}::{name}";
            }
            return new EocCmdInfo()
            {
                ReturnDataType = rawInfo.ReturnDataType == 0 ? null : EocDataTypes.Translate(P, rawInfo.ReturnDataType),
                CppName = cppName,
                Parameters = rawInfo.Parameters.Select((x) =>
                {
                    var dataType = EocDataTypes.Translate(P, x.DataType, x.ArrayParameter);
                    return new EocParameterInfo()
                    {
                        ByRef = x.ByRef || x.ArrayParameter || !EocDataTypes.IsValueType(dataType),
                        Optional = x.OptionalParameter,
                        VarArgs = false,
                        DataType = dataType,
                        CppName = P.GetUserDefinedName_SimpleCppName(x.Id)
                    };
                }).ToList()
            };
        }

        public CodeConverter(ProjectConverter projectConverter, EocClass classItem, MethodInfo methodItem)
        {
            this.P = projectConverter;
            this.Name = P.GetUserDefinedName_SimpleCppName(methodItem.Id);
            this.Info = InferEocCmdInfo(P, methodItem);
            this.IsClassMember = classItem is EocObjectClass;
            this.ClassItem = classItem;
            this.MethodItem = methodItem;
            this.ParamMap = new SortedDictionary<int, EocParameterInfo>();
            for (int i = 0; i < Info.Parameters.Count; i++)
            {
                this.ParamMap.Add(methodItem.Parameters[i].Id, Info.Parameters[i]);
            }
            this.LocalMap = methodItem.Variables.ToSortedDictionary(x => x.Id, x => new EocLocalVariableInfo()
            {
                CppName = P.GetUserDefinedName_SimpleCppName(x.Id),
                DataType = EocDataTypes.Translate(P, x.DataType, x.UBound),
                UBound = x.UBound.ToList()
            });
            if (IsClassMember)
            {
                this.RefId = $"{ClassItem.CppName}|{Info.CppName}";
            }
            else
            {
                this.RefId = Info.CppName;
            }
        }

        public void RemoveUnusedCode(HashSet<string> dependencies)
        {
            MethodItem.Variables = MethodItem.Variables.Where(x => dependencies.Contains($"{RefId}|{P.GetUserDefinedName_SimpleCppName(x.Id)}")).ToArray();
        }

        public void ParseCode()
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                this.StatementBlock = EocStatementBlock.Translate(this, CodeDataParser.ParseStatementBlock(MethodItem.CodeData.ExpressionData, MethodItem.CodeData.Encoding));
            }
        }

        private int TempVarId = 0;

        public string AllocTempVar()
        {
            return $"eoc_temp{TempVarId++}";
        }

        public CodeConverter Optimize()
        {
            StatementBlock = StatementBlock.Optimize() as EocStatementBlock;
            return this;
        }

        private void WriteOptionalParameterReader(CodeWriter writer)
        {
            foreach (var x in Info.Parameters.Where(x => x.Optional))
            {
                var name = x.CppName;
                var nullParameter = EocDataTypes.GetNullParameter(x.DataType);
                var initValue = EocDataTypes.GetInitValue(x.DataType);
                writer.NewLine();
                writer.Write($"bool eoc_isNull_{name} = !{name}.has_value();");
                if (x.ByRef)
                {
                    writer.NewLine();
                    if (string.IsNullOrWhiteSpace(nullParameter))
                    {
                        writer.Write($"{x.DataType} eoc_default_{name};");
                    }
                    else
                    {
                        writer.Write($"{x.DataType} eoc_default_{name}({nullParameter});");
                    }

                    writer.NewLine();
                    writer.Write($"{x.DataType}& eoc_value_{name} = eoc_isNull_{name} ? (eoc_default_{name} = {initValue}) : {name}.value().get();");
                }
                else
                {
                    writer.NewLine();
                    writer.Write($"{x.DataType} eoc_value_{name} = eoc_isNull_{name} ? {initValue} : {name}.value();");
                }
            }
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            P.AnalyzeDependencies(graph, Info, RefId);
            foreach (var x in LocalMap.Values)
            {
                var varRefId = $"{RefId}|{x.CppName}";
                P.AnalyzeDependencies(graph, varRefId, x.DataType);
            }
            StatementBlock.AnalyzeDependencies(graph);
        }

        internal void DefineItem(CodeWriter writer)
        {
            var isVirtual = false;
            if (IsClassMember)
            {
                var accessModifier = MethodItem.Public ? "public" : "protected";
                if (MethodItem.Name != "_初始化" && MethodItem.Name != "_销毁")
                {
                    isVirtual = true;
                }
                else
                {
                    accessModifier = "private";
                }
                writer.NewLine();
                writer.Write(accessModifier);
                writer.Write(":");
            }
            P.DefineMethod(writer, Info, Name, isVirtual);
        }

        internal void ImplementItem(CodeWriter writer)
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                string classRawName = null;
                if (ClassItem is EocObjectClass x)
                    classRawName = x.RawName;
                P.WriteMethodHeader(writer, Info, Name, false, classRawName, false);
                using (writer.NewBlock())
                {
                    P.DefineVariable(writer, null, LocalMap.Values);
                    WriteOptionalParameterReader(writer);
                    StatementBlock.WriteTo(writer);
                }
            }
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