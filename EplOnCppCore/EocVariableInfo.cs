using System;
using System.Collections.Generic;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocVariableInfo
    {
        public string CppName { get; set; }
        public CppTypeName DataType { get; set; }
        // 用于数组变量/可空数组参数的初始化
        public List<int> UBound { get; set; }
    }
}
