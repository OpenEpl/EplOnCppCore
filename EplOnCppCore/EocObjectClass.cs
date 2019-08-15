using QIQI.EProjectFile;
using QuickGraph;
using QuickGraph.Algorithms;
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
                BaseClassCppName = P.GetCppTypeName(rawInfo.BaseClass).ToString();
                BaseClassRawName = $"raw_{BaseClassName}";
                BaseClassRawCppName = $"{P.TypeNamespace}::eoc_internal::{BaseClassRawName}";
            }
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string,IEdge<string>> graph)
        {
            graph.AddVertex(RefId);
            if (BaseClassCppName != null)
                graph.AddVerticesAndEdge(new Edge<string>(RefId, BaseClassCppName));
            foreach (var x in RawInfo.Variables)
            {
                var varRefId = $"{RefId}|{P.GetUserDefinedName_SimpleCppName(x.Id)}";
                graph.AddVerticesAndEdge(new Edge<string>(RefId, varRefId));
                P.AnalyzeDependencies(graph, varRefId, P.GetCppTypeName(x));
            }
            foreach (var x in Method)
            {
                graph.AddVerticesAndEdge(new Edge<string>(RefId, x.RefId));
                x.AnalyzeDependencies(graph);
            }
        }

        public override void RemoveUnusedCode(HashSet<string> dependencies)
        {
            foreach (var item in Method)
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
                if (RawInfo.Variables.Length != 0)
                {
                    writer.Write("private:");
                    P.DefineVariable(writer, null, RawInfo.Variables, false);
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
                foreach (var item in Method)
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
                var initMethod = Method.Where(x => x.MethodItem.Name == "_初始化").FirstOrDefault();
                var destroyMethod = Method.Where(x => x.MethodItem.Name == "_销毁").FirstOrDefault();
                using (writer.NewNamespace("eoc_internal"))
                {
                    writer.NewLine();
                    writer.Write($"{RawName}::{RawName}()");
                    if (RawInfo.Variables.Length != 0)
                    {
                        writer.Write(": ");
                        P.InitMembersInConstructor(writer, RawInfo.Variables);
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

                    foreach (var item in Method)
                    {
                        item.ImplementItem(writer);
                    }
                }
            }
        }

        public static void DefineRawName(ProjectConverter P, CodeWriter writer, EocObjectClass[] collection)
        {
            //In e::user::type::eoc_internal
            foreach (var item in collection)
            {
                item.DefineRawName(writer);
            }
        }

        public static void DefineName(ProjectConverter P, CodeWriter writer, EocObjectClass[] collection)
        {
            //In e::user::type
            foreach (var item in collection)
            {
                item.DefineName(writer);
            }
        }

        public static void DefineRawObjectClass(ProjectConverter P, CodeWriter writer, EocObjectClass[] collection)
        {
            //In e::user::type::eoc_internal
            var map = collection.ToDictionary(x => x.CppName);
            var graph = new AdjacencyGraph<EocObjectClass, IEdge<EocObjectClass>>();
            foreach (var item in collection)
            {
                if (item.BaseClassCppName != null && map.TryGetValue(item.BaseClassCppName, out var baseEocClass))
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

        public static EocObjectClass[] Translate(ProjectConverter P, IEnumerable<ClassInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocObjectClass Translate(ProjectConverter P, ClassInfo rawInfo)
        {
            return new EocObjectClass(P, rawInfo);
        }
    }
}