using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class EocStaticClass : EocClass
    {
        public EocStaticClass(ProjectConverter p, ClassInfo rawInfo) : base(p, rawInfo)
        {
            MemberInfoMap = RawInfo.Variables.ToSortedDictionary(x => x.Id, x => new EocMemberInfo()
            {
                CppName = $"{this.CppName}::{P.GetUserDefinedName_SimpleCppName(x.Id)}",
                DataType = EocDataTypes.Translate(P, x.DataType, x.UBound),
                UBound = x.UBound.ToList(),
                Static = true
            });
        }

        public void Define(CodeWriter writer)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"../type.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                foreach (var item in Method.Values)
                {
                    item.DefineItem(writer);
                }
                foreach (var item in Method.Values)
                {
                    if (item.TemplatedMethod)
                    {
                        item.ImplementTemplateItem(writer);
                    }
                }
            }
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            foreach (var x in MemberInfoMap.Values)
            {
                var varRefId = x.CppName;
                P.AnalyzeDependencies(graph, varRefId, x.DataType);
            }
            foreach (var x in Method.Values)
            {
                x.AnalyzeDependencies(graph);
            }
        }

        public override void RemoveUnusedCode(HashSet<string> dependencies)
        {
            MemberInfoMap = MemberInfoMap.FilterSortedDictionary(x => dependencies.Contains(x.Value.CppName));
            Method = Method.FilterSortedDictionary(x => dependencies.Contains(x.Value.RefId));
            foreach (var item in Method.Values)
            {
                item.RemoveUnusedCode(dependencies);
            }
        }

        public void Implement(CodeWriter writer)
        {
            writer.Write("#include \"../../../stdafx.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                if (MemberInfoMap.Count > 0)
                {
                    using (writer.NewNamespace(Name))
                    {
                        P.DefineVariable(writer, new string[] { "static" }, MemberInfoMap.Values);
                    }
                }
                foreach (var item in Method.Values)
                {
                    if (!item.TemplatedMethod)
                    {
                        item.ImplementNormalItem(writer);
                    }
                }
            }
        }

        public static SortedDictionary<int, EocStaticClass> Translate(ProjectConverter P, IEnumerable<ClassInfo> rawInfos)
        {
            return rawInfos.ToSortedDictionary(x => x.Id, x => Translate(P, x));
        }

        public static EocStaticClass Translate(ProjectConverter P, ClassInfo rawInfo)
        {
            return new EocStaticClass(P, rawInfo);
        }
    }
}