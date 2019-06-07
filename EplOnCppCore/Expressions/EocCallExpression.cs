using QIQI.EProjectFile.Expressions;
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
            if (expr == null) return null;
            return new EocCallExpression(
                C,
                C.P.GetEocCmdInfo(expr),
                EocExpression.Translate(C, expr.Target),
                expr.ParamList?.Select(x => EocExpression.Translate(C, x)).ToList(),
                expr.LibraryId >= 0 ? C.P.EocLibs[expr.LibraryId]?.SuperTemplateAssembly : null);
        }

        public EocCallExpression(CodeConverter c, EocCmdInfo cmdInfo, EocExpression target, List<EocExpression> paramList, Assembly superTemplateAssembly = null) : base(c)
        {
            CmdInfo = cmdInfo;
            Target = target;
            ParamList = paramList;
            SuperTemplateAssembly = superTemplateAssembly;
        }

        public EocCmdInfo CmdInfo { get; }
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

        public override void WriteTo()
        {
            if (CmdInfo.SuperTemplate != null)
            {
                SuperTemplateAssembly
                    .GetType(CmdInfo.SuperTemplate.Class)
                    .GetMethod(
                        CmdInfo.SuperTemplate.Name,
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(CodeConverter), typeof(EocCallExpression) },
                        null)
                    .Invoke(null, new object[] { C, this });
                return;
            }

            if (Target != null)
            {
                Target.WriteTo();
                Writer.Write("->");
            }
            Writer.Write(CmdInfo.CppName);

            int lengthOfVarArgs = CmdInfo.GetLengthOfVarArgs();
            int startOfVarArgs = CmdInfo.Parameters.Count - lengthOfVarArgs;
            Writer.Write("(");
            for (int i = 0; i < ParamList.Count; i++)
            {
                var item = ParamList[i];
                if (i != 0)
                    Writer.Write(", ");
                EocParameterInfo eocParameterInfo = i < CmdInfo.Parameters.Count ? CmdInfo.Parameters[i] : CmdInfo.Parameters[startOfVarArgs + (i - startOfVarArgs) % lengthOfVarArgs];
                if (item == null)
                {
                    Writer.Write("std::nullopt");
                }
                else
                {
                    if (eocParameterInfo.ByRef)
                    {
                        if (eocParameterInfo.Optional)
                            Writer.Write("std::referenced_wrapper(");

                        Writer.Write("BYREF");
                        if (eocParameterInfo.DataType != ProjectConverter.CppTypeName_SkipCheck)
                        {
                            Writer.Write("(");
                            Writer.Write(eocParameterInfo.DataType.ToString());
                            Writer.Write(", ");
                        }
                        else
                        {
                            Writer.Write("_AUTO(");
                        }
                    }
                    item.WriteToWithCast(eocParameterInfo.DataType);
                    if (eocParameterInfo.ByRef)
                    {
                        if (eocParameterInfo.Optional)
                            Writer.Write(")");

                        Writer.Write(")");
                    }
                }
            }
            Writer.Write(")");
        }
    }
}