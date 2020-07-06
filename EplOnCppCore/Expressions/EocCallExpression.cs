using QIQI.EProjectFile.Expressions;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocCallExpression : EocExpression
    {
        public static EocCallExpression Translate(CodeConverter C, CallExpression expr)
        {
            var P = C.P;
            if (expr == null) return null;
            var result = new EocCallExpression(
                C,
                P.GetEocCmdInfo(expr),
                EocExpression.Translate(C, expr.Target),
                expr.ParamList?.Select(x => EocExpression.Translate(C, x)).ToList(),
                expr.LibraryId >= 0 ? P.EocLibs[expr.LibraryId]?.SuperTemplateAssembly : null);
            if (expr.InvokeSpecial)
            {
                result.SpecialScope = "raw_" + P.GetUserDefinedName_SimpleCppName(P.MethodIdToClassMap[expr.MethodId].Id);
            }
            return result;
        }

        public EocCallExpression(CodeConverter c, EocCmdInfo cmdInfo, EocExpression target, List<EocExpression> paramList, Assembly superTemplateAssembly = null) : base(c)
        {
            CmdInfo = cmdInfo;
            Target = target;
            ParamList = paramList;
            SuperTemplateAssembly = superTemplateAssembly;
        }

        public EocCmdInfo CmdInfo { get; }
        public string SpecialScope { get; set; }
        public EocExpression Target { get; set; }
        public List<EocExpression> ParamList { get; set; }
        public Assembly SuperTemplateAssembly { get; }
        public override CppTypeName GetResultType()
        {
            if (CmdInfo.SuperTemplateForReturnDataType != null)
            {
                return (CppTypeName)SuperTemplateAssembly
                    .GetType(CmdInfo.SuperTemplateForReturnDataType.Class)
                    .GetMethod(
                        CmdInfo.SuperTemplateForReturnDataType.Name,
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(CodeConverter), typeof(EocCallExpression) },
                        null)
                    .Invoke(null, new object[] { C, this });
            }
            return CmdInfo.ReturnDataType;
        }

        public override void WriteTo(CodeWriter writer)
        {
            if (CmdInfo.SuperTemplate != null)
            {
                SuperTemplateAssembly
                    .GetType(CmdInfo.SuperTemplate.Class)
                    .GetMethod(
                        CmdInfo.SuperTemplate.Name,
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(CodeConverter), typeof(CodeWriter), typeof(EocCallExpression) },
                        null)
                    .Invoke(null, new object[] { C, writer, this });
                return;
            }

            if (Target != null)
            {
                Target.WriteTo(writer);
                writer.Write("->");
            }
            else if (!string.IsNullOrEmpty(SpecialScope))
            {
                writer.Write(SpecialScope);
                writer.Write("::");
            }
            writer.Write(CmdInfo.CppName);

            int lengthOfVarArgs = CmdInfo.GetLengthOfVarArgs();
            int startOfVarArgs = CmdInfo.Parameters.Count - lengthOfVarArgs;
            writer.Write("(");
            for (int i = 0; i < ParamList.Count; i++)
            {
                var item = ParamList[i];
                if (i != 0)
                    writer.Write(", ");
                EocParameterInfo eocParameterInfo = i < CmdInfo.Parameters.Count ? CmdInfo.Parameters[i] : CmdInfo.Parameters[startOfVarArgs + (i - startOfVarArgs) % lengthOfVarArgs];
                if (item == null)
                {
                    writer.Write("std::nullopt");
                }
                else
                {
                    if (eocParameterInfo.ByRef)
                    {
                        if (eocParameterInfo.Optional)
                            writer.Write("std::reference_wrapper(");

                        writer.Write("BYREF");
                        if (eocParameterInfo.DataType != EocDataTypes.Auto)
                        {
                            writer.Write("(");
                            writer.Write(eocParameterInfo.DataType.ToString());
                            writer.Write(", ");
                        }
                        else
                        {
                            writer.Write("_AUTO(");
                        }
                    }
                    item.WriteToWithCast(writer, eocParameterInfo.DataType);
                    if (eocParameterInfo.ByRef)
                    {
                        if (eocParameterInfo.Optional)
                            writer.Write(")");

                        writer.Write(")");
                    }
                }
            }
            writer.Write(")");
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            if(Target != null)
            {
                if (deep)
                    Target.ProcessSubExpression(processor);
                Target = processor(Target);
            }
            for (int i = 0; i < ParamList.Count; i++)
            {
                if(ParamList[i] == null)
                    continue;
                if(deep)
                    ParamList[i].ProcessSubExpression(processor);
                ParamList[i] = processor(ParamList[i]);
            }
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            base.AnalyzeDependencies(graph);
            if(CmdInfo.CppName != null)
            {
                var refId = CmdInfo.CppName;
                if (Target != null)
                {
                    refId = Target.GetResultType().ToString() + "|" + refId;
                }
                graph.AddVerticesAndEdge(new Edge<string>(C.RefId, refId));
            }
        }
    }
}