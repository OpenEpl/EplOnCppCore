using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EplOnCpp.Core.Statements;
using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QIQI.EProjectFile.Expressions;
using QIQI.EProjectFile.Statements;
using QuikGraph;
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
        public StatementBlock RawStatementBlock { get; set; }
        public EocStatementBlock StatementBlock { get; set; }
        public bool TemplatedMethod { get; }

        private static EocCmdInfo InferEocCmdInfo(ProjectConverter P, MethodInfo rawInfo, StatementBlock rawStatementBlock)
        {
            var autoParam = new HashSet<int>();
            for (int i = 0; i < rawStatementBlock.Count; )
            {
                if (rawStatementBlock[i] is ExpressionStatement exprStat)
                {
                    var callExpr = exprStat.Expression;
                    if (callExpr != null && callExpr.LibraryId == P.EocHelperLibId)
                    {
                        var cmdName = P.IdToNameMap.GetLibCmdName(callExpr.LibraryId, callExpr.MethodId);
                        switch (cmdName)
                        {
                            case "EOC标记_自适应参数":
                                if (callExpr.ParamList.FirstOrDefault() is VariableExpression varExpr)
                                {
                                    autoParam.Add(varExpr.Id);
                                }
                                rawStatementBlock.RemoveAt(i);
                                continue;
                        }
                    }
                }
                i++;
            }

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
                    CppTypeName dataType;
                    if (autoParam.Contains(x.Id))
                    {
                        dataType = x.ArrayParameter ? EocDataTypes.ArrayOf(EocDataTypes.Auto) : EocDataTypes.Auto;
                    }
                    else
                    {
                        dataType = EocDataTypes.Translate(P, x.DataType, x.ArrayParameter);
                    }
                    if (dataType == EocDataTypes.Any && !x.ByRef)
                    {
                        //考虑到可空、参考（非基本类型强制参考、基本类型非参考）等问题，实现起来过于麻烦，暂时搁置
                        throw new NotImplementedException("暂不支持非参考自适应参数，请勾选 参考 或 贡献代码实现相应功能");
                    }
                    return new EocParameterInfo()
                    {
                        ByRef = x.ByRef || x.ArrayParameter || (!EocDataTypes.IsValueType(dataType) && dataType != EocDataTypes.Any),
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
            this.ClassItem = classItem;
            this.MethodItem = methodItem;
            this.RawStatementBlock = CodeDataParser.ParseStatementBlock(methodItem.CodeData.ExpressionData, methodItem.CodeData.Encoding);
            this.Info = InferEocCmdInfo(P, methodItem, RawStatementBlock);
            this.TemplatedMethod = Info.Parameters.Find(x => x.DataType == EocDataTypes.Auto) != null;
            this.IsClassMember = classItem is EocObjectClass;
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
            MethodItem.Variables = MethodItem.Variables.Where(x => dependencies.Contains($"{RefId}|{P.GetUserDefinedName_SimpleCppName(x.Id)}")).ToList();
        }

        public void ParseCode()
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                this.StatementBlock = EocStatementBlock.Translate(this, RawStatementBlock);
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
                var typeName = x.DataType == EocDataTypes.Auto ? $"_EocAutoParam_{x.CppName}" : x.DataType.ToString();
                var nullValue = $"e::system::default_value<{typeName}>::null()";
                var initValue = $"e::system::default_value<{typeName}>::value()";
                writer.NewLine();
                writer.Write($"bool eoc_isNull_{name} = !{name}.has_value();");
                if (x.ByRef)
                {
                    writer.NewLine();
                    writer.Write($"{typeName} eoc_default_{name} = {nullValue};");
                    writer.NewLine();
                    writer.Write($"{typeName}& eoc_value_{name} = eoc_isNull_{name} ? (eoc_default_{name} = {initValue}) : {name}.value().get();");
                }
                else
                {
                    writer.NewLine();
                    writer.Write($"{typeName} eoc_value_{name} = eoc_isNull_{name} ? {initValue} : {name}.value();");
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

        internal void ImplementNormalItem(CodeWriter writer)
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                if (TemplatedMethod)
                {
                    throw new Exception("无法使用模板化方法");
                }
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

        internal void ImplementTemplateItem(CodeWriter writer)
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                if (!TemplatedMethod)
                {
                    throw new Exception("必须使用模板化方法");
                }
                if (ClassItem is EocObjectClass x)
                {
                    throw new Exception("非静态方法不能模板化");
                }
                P.WriteMethodHeader(writer, Info, Name, false, null, false);
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