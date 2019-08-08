using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocEnv
    {
        public static string Home { get; set; }
        public static string LibPath => Home != null ? Path.Combine(Home, "lib") : null;
        public static bool IsValid => Home != null && File.Exists(Path.Combine(Home, "lib", "system", "config.cmake"));

        static EocEnv()
        {
            Home = null;

            if (Home == null)
            {
                try
                {
                    Home = Environment.GetEnvironmentVariable("EOC_HOME", EnvironmentVariableTarget.Process);
                }
                catch (Exception)
                {
                    Home = null;
                }
            }

            //如果exe启动后改变环境变量，读取进程环境变量需要重启exe得到值，而读取系统环境变量可立刻获取最新数据

            if (Home == null)
            {
                try
                {
                    Home = Environment.GetEnvironmentVariable("EOC_HOME", EnvironmentVariableTarget.User);
                }
                catch (Exception)
                {
                    Home = null;
                }
            }

            if (Home == null)
            {
                try
                {
                    Home = Environment.GetEnvironmentVariable("EOC_HOME", EnvironmentVariableTarget.Machine);
                }
                catch (Exception)
                {
                    Home = null;
                }
            }
        }
    }
}
