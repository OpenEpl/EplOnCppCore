using QIQI.EProjectFile;
using QIQI.EProjectFile.Expressions;
using QuickGraph;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocVariableExpression : EocExpression
    {
        public static EocVariableExpression Translate(CodeConverter C, VariableExpression expr)
        {
            if (expr == null) return null;
            switch (EplSystemId.GetType(expr.Id))
            {
                case EplSystemId.Type_Local:
                    if (C.ParamIdMap.TryGetValue(expr.Id, out var parameterInfo))
                    {
                        return new EocVariableExpression(C, parameterInfo);
                    }
                    var localVarInfo = C.LocalIdMap[expr.Id];
                    return new EocVariableExpression(C, localVarInfo);

                case EplSystemId.Type_ClassMember:
                    var classVar = C.P.ClassVarIdMap[expr.Id];
                    return new EocVariableExpression(C, classVar);

                case EplSystemId.Type_Global:
                    var globalVar = C.P.GlobalVarIdMap[expr.Id];
                    return new EocVariableExpression(C, globalVar);

                case int x:
                    throw new Exception("未知变量类型：0x" + x.ToString("X8"));
            }
        }

        public EocVariableExpression(CodeConverter c, AbstractVariableInfo variableInfo) : base(c)
        {
            VariableInfo = variableInfo;
        }

        public AbstractVariableInfo VariableInfo { get; }

        public override CppTypeName GetResultType()
        {
            return P.GetCppTypeName(VariableInfo);
        }

        public override void WriteTo(CodeWriter writer)
        {
            var name = P.GetUserDefinedName_SimpleCppName(VariableInfo.Id);
            switch (VariableInfo)
            {
                case MethodParameterInfo v:
                    if (v.OptionalParameter)
                    {
                        writer.Write("eoc_value_");
                    }
                    writer.Write(name);
                    break;

                case LocalVariableInfo v:
                    writer.Write(name);
                    break;

                case ClassVariableInfo v:
                    if (C.IsClassMember)
                    {
                        writer.Write("this->");
                    }
                    else
                    {
                        writer.Write(C.ClassItem.CppName);
                        writer.Write("::");
                    }
                    writer.Write(name);
                    break;

                case GlobalVariableInfo v:
                    writer.Write(P.GlobalNamespace);
                    writer.Write("::");
                    writer.Write(name);
                    break;

                default:
                    throw new Exception("未知变量访问：" + P.IdToNameMap.GetUserDefinedName(VariableInfo.Id));
            }
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            base.AnalyzeDependencies(graph);
            string varRefId;
            var name = P.GetUserDefinedName_SimpleCppName(VariableInfo.Id);
            switch (VariableInfo)
            {
                case MethodParameterInfo _:
                    varRefId = $"{C.RefId}|{name}";
                    break;

                case LocalVariableInfo _:
                    varRefId = $"{C.RefId}|{name}";
                    break;

                case ClassVariableInfo _:
                    if (C.IsClassMember)
                    {
                        varRefId = $"{C.ClassItem.CppName}|{name}";
                    }
                    else
                    {
                        varRefId = $"{C.ClassItem.CppName}::{name}";
                    }
                    break;

                case GlobalVariableInfo _:
                    varRefId = $"{P.GlobalNamespace}::{name}";
                    break;

                default:
                    throw new Exception("未知变量访问：" + P.IdToNameMap.GetUserDefinedName(VariableInfo.Id));
            }
            graph.AddVerticesAndEdge(new Edge<string>(C.RefId, varRefId));
        }
    }
}