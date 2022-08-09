using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using QuikGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class EocObjectClass : EocClass
    {
        public string RawName { get; }
        public string RawCppName { get; }
        public string BaseClassName { get; }
        public string BaseClassCppName { get; }
        public string BaseClassRawName { get; }
        public string BaseClassRawCppName { get; }

        public EocObjectClass(ProjectConverter p, ClassInfo rawInfo) : base(p, rawInfo)
        {
            RawName = "raw_" + Name;
            RawCppName = $"{P.TypeNamespace}::eoc_internal::{RawName}";
            if (rawInfo.BaseClass == 0 || rawInfo.BaseClass == -1)
            {
                BaseClassName = BaseClassCppName = BaseClassRawName = BaseClassRawCppName = null;
            }
            else
            {
                BaseClassName = P.GetUserDefinedName_SimpleCppName(rawInfo.BaseClass);
                BaseClassCppName = EocDataTypes.Translate(P, rawInfo.BaseClass).ToString();
                BaseClassRawName = $"raw_{BaseClassName}";
                BaseClassRawCppName = $"{P.TypeNamespace}::eoc_internal::{BaseClassRawName}";
            }
            MemberInfoMap = RawInfo.Variables.ToSortedDictionary(x => x.Id, x => new EocMemberInfo()
            {
                CppName = P.GetUserDefinedName_SimpleCppName(x.Id),
                DataType = EocDataTypes.Translate(P, x.DataType, x.UBound),
                UBound = x.UBound.ToList()
            });
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            graph.AddVertex(RefId);
            if (BaseClassCppName != null)
                graph.AddVerticesAndEdge(new Edge<string>(RefId, BaseClassCppName));
            foreach (var x in MemberInfoMap.Values)
            {
                var varRefId = $"{RefId}|{x.CppName}";
                graph.AddVerticesAndEdge(new Edge<string>(RefId, varRefId));
                P.AnalyzeDependencies(graph, varRefId, x.DataType);
            }
            foreach (var x in Method.Values)
            {
                graph.AddVerticesAndEdge(new Edge<string>(RefId, x.RefId));
                x.AnalyzeDependencies(graph);
            }
        }

        public override void RemoveUnusedCode(HashSet<string> dependencies)
        {
            foreach (var item in Method.Values)
            {
                item.RemoveUnusedCode(dependencies);
            }
        }

        private void DefineRawName(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"class {RawName};");
        }

        private void DefineName(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"typedef e::system::object_ptr<{RawCppName}> {Name};");
        }

        private void DefineRawObjectClass(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"class {RawName}");
            if (BaseClassRawCppName != null)
            {
                writer.Write(": public ");
                writer.Write(BaseClassRawCppName);
            }
            else
            {
                writer.Write(": public e::system::basic_object");
            }
            using (writer.NewBlock())
            {
                writer.NewLine();
                if (MemberInfoMap.Count != 0)
                {
                    writer.Write("private:");
                    P.DefineVariable(writer, null, MemberInfoMap.Values, false);
                }
                writer.NewLine();
                writer.Write("public:");
                writer.NewLine();
                writer.Write($"{RawName}();");
                writer.NewLine();
                writer.Write($"{RawName}(const {RawName}&);");
                writer.NewLine();
                writer.Write($"virtual ~{RawName}();");
                writer.NewLine();
                writer.Write($"virtual e::system::basic_object* __stdcall clone();");
                foreach (var item in Method.Values)
                {
                    item.DefineItem(writer);
                }
            }
            writer.Write(";");
        }

        public void ImplementRawObjectClass(CodeWriter writer)
        {
            writer.Write("#include \"../../../stdafx.h\"");
            using (writer.NewNamespace(P.TypeNamespace))
            {
                var initMethod = Method.Values.Where(x => x.MethodItem.Name == "_初始化").FirstOrDefault();
                var destroyMethod = Method.Values.Where(x => x.MethodItem.Name == "_销毁").FirstOrDefault();
                using (writer.NewNamespace("eoc_internal"))
                {
                    writer.NewLine();
                    writer.Write($"{RawName}::{RawName}()");
                    if (MemberInfoMap.Count != 0)
                    {
                        writer.Write(": ");
                        P.InitMembersInConstructor(writer, MemberInfoMap.Values);
                    }
                    using (writer.NewBlock())
                    {
                        if (initMethod != null)
                        {
                            writer.NewLine();
                            writer.Write($"this->{initMethod.Name}();");
                        }
                    }
                    writer.NewLine();
                    writer.Write($"{RawName}::~{RawName}()");
                    using (writer.NewBlock())
                    {
                        if (destroyMethod != null)
                        {
                            writer.NewLine();
                            writer.Write($"this->{destroyMethod.Name}();");
                        }
                    }

                    writer.NewLine();
                    writer.Write($"{RawName}::{RawName}(const {RawName}&) = default;");

                    writer.NewLine();
                    writer.Write($"e::system::basic_object* {RawName}::clone()");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write($"return new {RawName}(*this);");
                    }

                    foreach (var item in Method.Values)
                    {
                        item.ImplementNormalItem(writer);
                    }
                }
            }
        }

        public static void DefineRawName(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocObjectClass> map)
        {
            //In e::user::type::eoc_internal
            foreach (var item in map.Values)
            {
                item.DefineRawName(writer);
            }
        }

        public static void DefineName(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocObjectClass> map)
        {
            //In e::user::type
            foreach (var item in map.Values)
            {
                item.DefineName(writer);
            }
        }

        public static void DefineRawObjectClass(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocObjectClass> map)
        {
            //In e::user::type::eoc_internal
            var nameMap = map.ToDictionary(x => x.Value.CppName, x => x.Value);
            var graph = new AdjacencyGraph<EocObjectClass, IEdge<EocObjectClass>>();
            foreach (var item in map.Values)
            {
                if (item.BaseClassCppName != null && nameMap.TryGetValue(item.BaseClassCppName, out var baseEocClass))
                {
                    graph.AddVerticesAndEdge(new Edge<EocObjectClass>(baseEocClass, item));
                }
                else
                {
                    graph.AddVertex(item);
                }
            }

            foreach (var item in graph.TopologicalSort())
            {
                item.DefineRawObjectClass(writer);
            }
        }

        public static SortedDictionary<int, EocObjectClass> Translate(ProjectConverter P, IEnumerable<ClassInfo> rawInfos)
        {
            return rawInfos.ToSortedDictionary(x => x.Id, x => Translate(P, x));
        }

        public static EocObjectClass Translate(ProjectConverter P, ClassInfo rawInfo)
        {
            return new EocObjectClass(P, rawInfo);
        }
    }
}