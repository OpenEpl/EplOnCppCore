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
                    if (C.ParamMap.TryGetValue(expr.Id, out var parameterInfo))
                    {
                        return new EocVariableExpression(C, parameterInfo);
                    }
                    var localVarInfo = C.LocalMap[expr.Id];
                    return new EocVariableExpression(C, localVarInfo);

                case EplSystemId.Type_ClassMember:
                    var classVar = C.P.EocMemberMap[expr.Id];
                    return new EocVariableExpression(C, classVar);

                case EplSystemId.Type_Global:
                    var globalVar = C.P.EocGlobalVariableMap[expr.Id].Info;
                    return new EocVariableExpression(C, globalVar);

                case int x:
                    throw new Exception("未知变量类型：0x" + x.ToString("X8"));
            }
        }

        public EocVariableExpression(CodeConverter c, EocVariableInfo variableInfo) : base(c)
        {
            VariableInfo = variableInfo;
        }

        public EocVariableInfo VariableInfo { get; }

        public override CppTypeName GetResultType()
        {
            return VariableInfo.DataType;
        }

        public override void WriteTo(CodeWriter writer)
        {
            switch (VariableInfo)
            {
                case EocMemberInfo x when !x.Static:
                    if (C.IsClassMember)
                    {
                        writer.Write("this->");
                    }
                    break;
                case EocParameterInfo x when x.Optional:
                    writer.Write("eoc_value_");
                    break;
                default:
                    break;
            }
            writer.Write(VariableInfo.CppName);
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            base.AnalyzeDependencies(graph);
            string varRefId;
            switch (VariableInfo)
            {
                case EocMemberInfo x when !x.Static:
                    varRefId = $"{C.ClassItem.RefId}|{x.CppName}";
                    break;
                case EocParameterInfo x:
                    varRefId = $"{C.RefId}|{x.CppName}";
                    break;
                case EocLocalVariableInfo x:
                    varRefId = $"{C.RefId}|{x.CppName}";
                    break;
                default:
                    varRefId = VariableInfo.CppName;
                    break;
            }
            graph.AddVerticesAndEdge(new Edge<string>(C.RefId, varRefId));
        }
    }
}