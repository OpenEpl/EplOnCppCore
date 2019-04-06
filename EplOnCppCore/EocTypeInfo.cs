using System.Collections.Generic;

namespace QIQI.EplOnCpp.Core
{
    public class EocTypeInfo
    {
        public CppTypeName CppName { get; set; }
        public Dictionary<string, EocCmdInfo> Method { get; set; }
        public Dictionary<string, EocMemberInfo> Member { get; set; }
    }
}