using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace QIQI.EplOnCpp.Core
{
    public class EocConstantInfo
    {
        [JsonIgnore]
        [YamlIgnore]
        public string RefId => CppName ?? Getter;
        public string CppName { get; set; }
        public string Getter { get; set; }
        public CppTypeName DataType { get; set; } = EocDataTypes.Int;
        public object Value { get; set; }

        private static readonly Dictionary<CppTypeName, Func<object, object>> ProcessorForNormalization = new Dictionary<CppTypeName, Func<object, object>> {
            { EocDataTypes.Bool, x => Convert.ToBoolean(x) },
            { EocDataTypes.Byte, x => Convert.ToByte(x) },
            { EocDataTypes.Short, x => Convert.ToInt16(x) },
            { EocDataTypes.Int, x => Convert.ToInt32(x) },
            { EocDataTypes.Long, x => Convert.ToInt64(x) },
            { EocDataTypes.Float, x => Convert.ToSingle(x) },
            { EocDataTypes.Double, x => Convert.ToDouble(x) },
            { EocDataTypes.String, x => Convert.ToString(x) }
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