using QIQI.EProjectFile;
using QuickGraph;
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
        public List<CodeConverter> Method { get; }

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
            Method = RawInfo.Method.Select(x => P.MethodIdMap[x]).Select(x => new CodeConverter(P, this, x)).ToList();
        }

        public void ParseCode()
        {
            foreach (var item in Method)
            {
                item.ParseCode();
            }
        }

        public void Optimize()
        {
            for (int i = 0; i < Method.Count; i++)
            {
                Method[i] = Method[i].Optimize();
            }
        }
    }
}