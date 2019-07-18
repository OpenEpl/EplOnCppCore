using QIQI.EProjectFile;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocGlobalVariable
    {
        public string RefId => CppName;
        public ProjectConverter P { get; }
        public string Name { get; }
        public string CppName { get; }
        public GlobalVariableInfo RawInfo { get; }

        public EocGlobalVariable(ProjectConverter p, GlobalVariableInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
            Name = P.GetUserDefinedName_SimpleCppName(RawInfo.Id);
            CppName = $"{P.GlobalNamespace}::{Name}";
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            graph.AddVertex(RefId);
            P.AnalyzeDependencies(graph, RefId, P.GetCppTypeName(RawInfo));
        }

        private void DefineItem(CodeWriter writer)
        {
            P.DefineVariable(writer, new string[] { "extern" }, RawInfo, false);
        }

        private void ImplementItem(CodeWriter writer)
        {
            P.DefineVariable(writer, null, RawInfo);
        }

        public static EocGlobalVariable[] Translate(ProjectConverter P, IEnumerable<GlobalVariableInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocGlobalVariable Translate(ProjectConverter P, GlobalVariableInfo rawInfo)
        {
            return new EocGlobalVariable(P, rawInfo);
        }

        public static void Define(ProjectConverter P, CodeWriter writer, EocGlobalVariable[] collection)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"type.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in collection)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, EocGlobalVariable[] collection)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"global.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in collection)
                {
                    item.ImplementItem(writer);
                }
            }
        }
    }
}
