using System.Collections.Generic;

namespace QIQI.EplOnCpp.Core
{
    public class EocCmdInfo
    {
        public CppTypeName ReturnDataType { get; set; }
        public string CppName { get; set; }
        public List<EocParameterInfo> Parameters { get; set; }
        public string AccessOperator { get; set; }
        public EocSuperTemplateInfo SuperTemplate { get; set; }
        public EocSuperTemplateInfo SuperTemplateForReturnDataType { get; set; }

        public int GetLengthOfVarArgs()
        {
            int c = 0;
            for (int i = Parameters.Count - 1; i >= 0; i--)
            {
                if (!Parameters[i].VarArgs)
                    break;
                c++;
            }
            return c;
        }
    }
}