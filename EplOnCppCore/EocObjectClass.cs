using QIQI.EProjectFile;
using QuickGraph;
using QuickGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class EocObjectClass
    {
        public ProjectConverter P { get; }

        public ClassInfo RawInfo { get; }
        public string Name { get; }
        public string CppName { get; }
        public string RawName { get; }
        public string RawCppName { get; }
        public string BaseClassName { get; }
        public string BaseClassCppName { get; }
        public string BaseClassRawName { get; }
        public string BaseClassRawCppName { get; }
        public List<CodeConverter> Method { get; }

        public EocObjectClass(ProjectConverter p, ClassInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
            Name = P.GetUserDefinedName_SimpleCppName(RawInfo.Id);
            RawName = "raw_" + Name;
            RawCppName = $"{P.TypeNamespace}::eoc_internal::{RawName}";
            CppName = P.GetCppTypeName(rawInfo.Id).ToString();
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
            Method = RawInfo.Method.Select(x => P.MethodIdMap[x]).Select(x => new CodeConverter(P, RawInfo, x)).ToList();
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
                bool hasInitMethod = Method.Where(x => x.MethodItem.Name == "_初始化").FirstOrDefault() != null;
                bool hasDestroyMethod = Method.Where(x => x.MethodItem.Name == "_销毁").FirstOrDefault() != null;
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
                        if (hasInitMethod)
                        {
                            writer.NewLine();
                            writer.Write("this->_初始化();");
                        }
                    }
                    writer.NewLine();
                    writer.Write($"{RawName}::~{RawName}()");
                    using (writer.NewBlock())
                    {
                        if (hasDestroyMethod)
                        {
                            writer.NewLine();
                            writer.Write("this->_销毁();");
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