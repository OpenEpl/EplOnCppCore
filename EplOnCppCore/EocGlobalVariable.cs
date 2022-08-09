using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocGlobalVariable
    {
        public string RefId => Info.CppName;
        public ProjectConverter P { get; }
        public EocVariableInfo Info { get; }

        public EocGlobalVariable(ProjectConverter p, EocVariableInfo info)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            graph.AddVertex(RefId);
            P.AnalyzeDependencies(graph, RefId, Info.DataType);
        }

        private void DefineItem(CodeWriter writer)
        {
            P.DefineVariable(writer, new string[] { "extern" }, Info, false);
        }

        private void ImplementItem(CodeWriter writer)
        {
            P.DefineVariable(writer, null, Info);
        }

        public static SortedDictionary<int, EocGlobalVariable> Translate(ProjectConverter P, IEnumerable<GlobalVariableInfo> rawInfos)
        {
            return rawInfos.ToSortedDictionary(x => x.Id, x => Translate(P, x));
        }

        public static EocGlobalVariable Translate(ProjectConverter P, GlobalVariableInfo x)
        {
            return new EocGlobalVariable(P, new EocVariableInfo()
            {
                CppName = $"{P.GlobalNamespace}::{P.GetUserDefinedName_SimpleCppName(x.Id)}",
                DataType = EocDataTypes.Translate(P, x.DataType, x.UBound),
                UBound = x.UBound.ToList()
            });
        }

        public static void Define(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocGlobalVariable> map)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"type.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in map.Values)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocGlobalVariable> map)
        {
            writer.Write("#include \"global.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in map.Values)
                {
                    item.ImplementItem(writer);
                }
            }
        }
    }
}
