using System;
using System.Collections.Generic;

namespace QIQI.EplOnCpp.Core
{
    public class EocConstantInfo
    {
        public string CppName { get; set; }
        public string Getter { get; set; }
        public CppTypeName DataType { get; set; } = ProjectConverter.CppTypeName_Int;
        public object Value { get ; set; }

        private static readonly Dictionary<CppTypeName, Func<object, object>> ProcessorForNormalization = new Dictionary<CppTypeName, Func<object, object>> {
            { ProjectConverter.CppTypeName_Bool, x => Convert.ToBoolean(x) },
            { ProjectConverter.CppTypeName_Byte, x => Convert.ToByte(x) },
            { ProjectConverter.CppTypeName_Short, x => Convert.ToInt16(x) },
            { ProjectConverter.CppTypeName_Int, x => Convert.ToInt32(x) },
            { ProjectConverter.CppTypeName_Long, x => Convert.ToInt64(x) },
            { ProjectConverter.CppTypeName_Float, x => Convert.ToSingle(x) },
            { ProjectConverter.CppTypeName_Double, x => Convert.ToDouble(x) },
            { ProjectConverter.CppTypeName_String, x => Convert.ToString(x) }
        };

        public void Normalize()
        {
            if (this.Value != null
                && ProcessorForNormalization.TryGetValue(DataType, out var processor))
            {
                this.Value = processor(this.Value);
            }
        }
    }
}