using CommandLine;
using QIQI.EplOnCpp.Core;
using QIQI.EProjectFile;
using QIQI.EProjectFile.Sections;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace QIQI.EplOnCpp.CLI
{
    [Verb("convert", HelpText = "Convert *.ec file to CMake Project")]
    internal class ConvertOptions
    {
        [Value(0, Required = true, HelpText = "Set the input file (*.ec)")]
        public string Input { get; set; }

        [Value(1, Required = true, HelpText = "Set the output directory (CMake Project)")]
        public string Output { get; set; }

        [Option("type", Default = ProjectConverter.EocProjectType.Windows, HelpText = "Set the project type", Required = false)]
        public ProjectConverter.EocProjectType Type { get; set; }

        [Option("force", Default = false, HelpText = "Set this flag when you want to input *.e file (MUST NOT have any unexamined statements or ECom references)", Required = false)]
        public bool Force { get; set; }

        [Option("debug", Default = false, HelpText = "Set this flag when you want to see debug info", Required = false)]
        public bool Debug { get; set; }

        public int Run()
        {
            var source = new EplDocument();
            var logger = new StreamLoggerWithContext(Console.Out, Console.Error, Debug);
            source.Load(File.OpenRead(Input));
            if (!Force && source.GetOrNull(ESystemInfoSection.Key)?.FileType != 3)
                throw new Exception("源文件应为ECom(*.ec)文件");
            new ProjectConverter(source, Type, default, logger).Generate(Output);
            return 0;
        }
    }

    [Verb("version", Hidden = true)]
    internal class VersionOptions
    {
        public int Run()
        {
            var coreVersionInfo = Attribute.GetCustomAttribute(typeof(ProjectConverter).Assembly, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;

            var cliVersionInfo = Attribute.GetCustomAttribute(typeof(Program).Assembly, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;

            Console.WriteLine("Eoc Home: {0}", EocEnv.Home);
            Console.WriteLine("Eoc Lib Path: {0}", EocEnv.LibPath);
            Console.WriteLine("Is Valid: {0}", EocEnv.IsValid);
            Console.WriteLine();
            Console.WriteLine("Core Version: {0}", coreVersionInfo?.InformationalVersion ?? "Unknown");
            Console.WriteLine("CLI Version: {0}", cliVersionInfo?.InformationalVersion ?? "Unknown");

            return 0;
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return new Parser(x =>
            {
                x.CaseSensitive = false;
                x.CaseInsensitiveEnumValues = true;
                x.AutoVersion = false;
                x.HelpWriter = Console.Error;
            }).ParseArguments<ConvertOptions, VersionOptions>(args)
              .MapResult(
                options => (options as dynamic).Run(),
                _ => 1);
        }
    }
}