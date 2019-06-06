using Newtonsoft.Json;
using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
namespace QIQI.EplOnCpp.Core
{
    public class EocLibInfo
    {
        public static EocLibInfo Load(LibraryRefInfo refInfo)
        {
            var curLibInfoPath = Path.Combine(EocEnv.LibPath, refInfo.FileName);
            var result = JsonConvert.DeserializeObject<EocLibInfo>(File.OpenText(Path.Combine(curLibInfoPath, "info.json")).ReadToEnd());

            if (result.Constant != null)
            {
                foreach (var item in result.Constant.Values)
                {
                    item.Normalize();
                }
            }
            if (result.Enum != null)
            {
                foreach (var item in result.Enum.Values.SelectMany(x => x.Values))
                {
                    item.Normalize();
                }
            }

            if (!string.IsNullOrEmpty(result.SuperTemplate))
            {
                try
                {
                    result.SuperTemplateAssembly = Assembly.LoadFrom(Path.Combine(curLibInfoPath, result.SuperTemplate));
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        public string SuperTemplate { get; set; }
        public Dictionary<string, EocCmdInfo> Cmd { get; set; }
        public Dictionary<string, EocTypeInfo> Type { get; set; }
        public Dictionary<string, EocConstantInfo> Constant { get; set; }
        public Dictionary<string, Dictionary<string, EocConstantInfo>> Enum { get; set; }

        [JsonIgnore]
        public Assembly SuperTemplateAssembly { get; private set; }

        public string CMakeName { get; set; }
    }
}