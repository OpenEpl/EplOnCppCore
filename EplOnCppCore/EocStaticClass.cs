using QIQI.EProjectFile;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class EocStaticClass: EocClass
    {

        public EocStaticClass(ProjectConverter p, ClassInfo rawInfo) : base(p, rawInfo)
        {
        }
        public void Define(CodeWriter writer)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"../type.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                foreach (var item in Method)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            foreach (var x in RawInfo.Variables)
            {
                var varRefId = $"{CppName}::{P.GetUserDefinedName_SimpleCppName(x.Id)}";
                P.AnalyzeDependencies(graph, varRefId, P.GetCppTypeName(x));
            }
            foreach (var x in Method)
            {
                x.AnalyzeDependencies(graph);
            }
        }

        public void Implement(CodeWriter writer)
        {
            writer.Write("#include \"../../../stdafx.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                if (RawInfo.Variables.Length > 0)
                {
                    using (writer.NewNamespace(Name))
                    {
                        P.DefineVariable(writer, new string[] { "static" }, RawInfo.Variables);
                    }
                }
                foreach (var item in Method)
                {
                    item.ImplementItem(writer);
                }
            }
        }

        public static EocStaticClass[] Translate(ProjectConverter P, IEnumerable<ClassInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocStaticClass Translate(ProjectConverter P, ClassInfo rawInfo)
        {
            return new EocStaticClass(P, rawInfo);
        }
    }
}