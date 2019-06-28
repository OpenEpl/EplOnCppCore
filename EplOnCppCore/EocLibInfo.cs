using Newtonsoft.Json;
using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using YamlDotNet.Serialization;
namespace QIQI.EplOnCpp.Core
{
    public class EocLibInfo
    {
        public static EocLibInfo Load(LibraryRefInfo refInfo)
        {
            var curLibInfoPath = Path.Combine(EocEnv.LibPath, refInfo.FileName);
            EocLibInfo result;
            if (File.Exists(Path.Combine(curLibInfoPath, "info.json")))
            {
                result = JsonConvert.DeserializeObject<EocLibInfo>(File.OpenText(Path.Combine(curLibInfoPath, "info.json")).ReadToEnd());
            }
            else if(File.Exists(Path.Combine(curLibInfoPath, "info.yml")))
            {
                result = new Deserializer().Deserialize<EocLibInfo>(File.OpenText(Path.Combine(curLibInfoPath, "info.yml")).ReadToEnd());
            }
            else
            {
                throw new Exception($"未在目录{curLibInfoPath}下找到库信息文件");
            }

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
        [YamlIgnore]
        public Assembly SuperTemplateAssembly { get; private set; }

        public string CMakeName { get; set; }
    }
}