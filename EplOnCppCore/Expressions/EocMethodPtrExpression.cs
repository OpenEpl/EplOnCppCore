using QIQI.EProjectFile.Expressions;
using System.Linq;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocMethodPtrExpression : EocExpression
    {
        public static EocMethodPtrExpression Translate(CodeConverter C, MethodPtrExpression expr)
        {
            if (expr == null) return null;
            return new EocMethodPtrExpression(C, C.P.GetEocCmdInfo(expr));
        }

        public EocMethodPtrExpression(CodeConverter c, EocCmdInfo cmdInfo) : base(c)
        {
            CmdInfo = cmdInfo;
        }

        public EocCmdInfo CmdInfo { get; }

        public override CppTypeName GetResultType()
        {
            return ProjectConverter.CppTypeName_MethodPtr;
        }

        public override void WriteTo(CodeWriter writer)
        {
            writer.Write("e::system::MethodPtrPackager<");
            writer.Write(CmdInfo.ReturnDataType == null ? "void" : CmdInfo.ReturnDataType.ToString());
            writer.Write("(");
            writer.Write(string.Join(", ", CmdInfo.Parameters.Select(x => P.GetParameterTypeString(x))));
            writer.Write(")");
            writer.Write(">::ptr<&" + CmdInfo.CppName + ">");
        }
    }
}