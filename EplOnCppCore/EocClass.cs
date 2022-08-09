using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public abstract class EocClass
    {
        public string RefId => CppName;
        public ProjectConverter P { get; }
        public ClassInfo RawInfo { get; }
        public string Name { get; }
        public string CppName { get; }
        public SortedDictionary<int, CodeConverter> Method { get; set; }
        public SortedDictionary<int, EocMemberInfo> MemberInfoMap { get; set; }

        public abstract void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph);
        public abstract void RemoveUnusedCode(HashSet<string> dependencies);

        public EocClass(ProjectConverter p, ClassInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
            Name = P.GetUserDefinedName_SimpleCppName(RawInfo.Id);
            if (EplSystemId.GetType(rawInfo.Id) == EplSystemId.Type_Class)
            {
                CppName = $"{P.TypeNamespace}::{Name}";
            }
            else
            {
                CppName = $"{P.CmdNamespace}::{Name}";
            }
            Method = RawInfo.Method.Select(x => P.MethodIdMap[x]).ToSortedDictionary(x => x.Id, x => new CodeConverter(P, this, x));
        }

        public void ParseCode()
        {
            foreach (var item in Method.Values)
            {
                item.ParseCode();
            }
        }

        public void Optimize()
        {
            var keys = Method.Keys.ToList();
            foreach (var id in keys)
            {
                Method[id] = Method[id].Optimize();
            }
        }
    }
}