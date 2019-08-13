using QIQI.EProjectFile;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace QIQI.EplOnCpp.Core
{
    public class EocDllExport
    {
        public ProjectConverter P { get; }
        public string Name { get; }
        public EocCmdInfo Info { get; }
        public string RefId => Info?.CppName;
        public string ExportId { get; }
        public EocDllExport(ProjectConverter p, string name, EocCmdInfo info, string exportId)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Info = info ?? throw new ArgumentNullException(nameof(info));
            ExportId = exportId ?? Name;
        }
        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            graph.AddVerticesAndEdge(new Edge<string>("[Root]", RefId));
        }
        private void ImplementItem(ProjectConverter P, CodeWriter writer)
        {
            var paramName = P.GetParamNameFromInfo(Info.Parameters);
            writer.NewLine();
            writer.Write(Info.ReturnDataType == null ? "void" : $"typename e::system::MethodPtrPackager_Result<{Info.ReturnDataType}>::NativeType");
            writer.Write(" __stdcall ");
            writer.Write("eoc_export_");
            writer.Write(ExportId);
            writer.Write("(");
            for (int i = 0; i < Info.Parameters.Count; i++)
            {
                if (i != 0)
                    writer.Write(", ");
                writer.Write("typename MethodPtrPackager_Arg<");
                writer.Write(P.GetParameterTypeString(Info.Parameters[i]));
                writer.Write(">::NativeType ");
                writer.Write(paramName[i]);
            }
            writer.Write(")");
            using(writer.NewBlock())
            {
                writer.NewLine();
                writer.Write("return e::system::MethodPtrPackager<");
                writer.Write(Info.ReturnDataType == null ? "void" : Info.ReturnDataType.ToString());
                writer.Write("(");
                writer.Write(string.Join(", ", Info.Parameters.Select(x => P.GetParameterTypeString(x))));
                writer.Write(")");
                writer.Write(">::func<&" + Info.CppName + ">");
                writer.Write("(");
                writer.Write(string.Join(", ", paramName));
                writer.Write(");");
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, EocDllExport[] dllExports)
        {
            writer.Write("#include \"stdafx.h\"");
            writer.NewLine();
            writer.Write("extern \"C\"");
            using (writer.NewBlock())
            {
                foreach (var item in dllExports)
                {
                    item.ImplementItem(P, writer);
                }
            }
        }

        private void MakeDefItem(ProjectConverter p, StreamWriter writer)
        {
            writer.WriteLine($"{Name} = eoc_export_{ExportId}");
        }

        public static void MakeDef(ProjectConverter P, StreamWriter writer, EocDllExport[] dllExports)
        {
            writer.WriteLine("EXPORTS");
            foreach (var item in dllExports)
            {
                item.MakeDefItem(P, writer);
            }
        }

        public static EocDllExport[] Translate(ProjectConverter P, IEnumerable<MethodInfo> methods)
        {
            return methods.Select(x => Translate(P, x)).ToArray();
        }
        public static EocDllExport Translate(ProjectConverter P, MethodInfo methodInfo)
        {
            return new EocDllExport(P, P.IdToNameMap.GetUserDefinedName(methodInfo.Id), P.GetEocCmdInfo(methodInfo), methodInfo.Id.ToString("X8"));
        }
    }
}
