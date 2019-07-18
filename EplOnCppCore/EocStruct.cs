using QIQI.EProjectFile;
using QuickGraph;
using QuickGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocStruct
    {

        public ProjectConverter P { get; }

        public StructInfo RawInfo { get; }
        public string Name { get;  }
        public string CppName { get; }
        public string RawName { get; }
        public string RawCppName { get; }

        public EocStruct(ProjectConverter p, StructInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
            Name = P.GetUserDefinedName_SimpleCppName(RawInfo.Id);
            RawName = "raw_" + Name;
            RawCppName = $"{P.TypeNamespace}::eoc_internal::{RawName}";
            CppName = P.GetCppTypeName(rawInfo.Id).ToString();
        }
        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            graph.AddVertex(CppName);
            foreach (var x in RawInfo.Member)
            {
                var varRefId = $"{CppName}|{P.GetUserDefinedName_SimpleCppName(x.Id)}";
                graph.AddVerticesAndEdge(new Edge<string>(CppName, varRefId));
                P.AnalyzeDependencies(graph, varRefId, P.GetCppTypeName(x));
            }
        }

        private void DefineRawName(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"struct {RawName};");
        }
        private void DefineName(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"typedef e::system::struct_ptr<{RawCppName}> {Name};");
        }

        private void DefineRawStructInfo(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write($"struct {RawName}");
            using (writer.NewBlock())
            {
                P.DefineVariable(writer, null, RawInfo.Member, false);

                writer.NewLine();
                writer.Write($"{RawName}()");
                if (RawInfo.Member.Length != 0)
                {
                    writer.Write(": ");
                    P.InitMembersInConstructor(writer, RawInfo.Member);
                }
                using (writer.NewBlock())
                {
                }
            }
            writer.Write(";");
        }

        private void DefineStructMarshaler(CodeWriter writer)
        {
            writer.NewLine();
            writer.Write("template<> struct marshaler<");
            writer.Write(CppName);
            writer.Write(">");
            using (writer.NewBlock())
            {
                writer.NewLine();
                writer.Write("private: ");

                writer.NewLine();
                writer.Write("using ManagedType = ");
                writer.Write(CppName);
                writer.Write(";");

                writer.NewLine();
                writer.Write("public: ");

                writer.NewLine();
                writer.Write("static constexpr bool SameMemoryStruct = false;");

                writer.NewLine();
                writer.Write("#pragma pack(push)");
                writer.NewLine();
                writer.Write("#pragma pack(1)");

                writer.NewLine();
                writer.Write("struct NativeType");
                WriteStructMarshalerCodeBlock(writer, "DefineMember");
                writer.Write(";");

                writer.NewLine();
                writer.Write("#pragma pack(pop)");

                writer.NewLine();
                writer.Write("static void marshal(NativeType &v, ManagedType &r)");
                WriteStructMarshalerCodeBlock(writer, "MarshalMember");
                writer.Write(";");

                writer.NewLine();
                writer.Write("static void cleanup(NativeType &v, ManagedType &r)");
                WriteStructMarshalerCodeBlock(writer, "CleanupMember");
                writer.Write(";");
            }
            writer.Write(";");
        }

        private void WriteStructMarshalerCodeBlock(CodeWriter writer, string cmd)
        {
            using (writer.NewBlock())
            {
                foreach (var member in RawInfo.Member)
                {
                    var memberCppName = P.GetUserDefinedName_SimpleCppName(member.Id);
                    writer.NewLine();
                    if (member.ByRef)
                        writer.Write($"StructMarshaler_{cmd}_Ref(ManagedType, {memberCppName});");
                    else if (member.UBound != null && member.UBound.Length != 0)
                        writer.Write($"StructMarshaler_{cmd}_Array(ManagedType, {memberCppName}, {P.CalculateArraySize(member.UBound)});");
                    else
                        writer.Write($"StructMarshaler_{cmd}(ManagedType, {memberCppName});");
                }
            }
        }
        public static void DefineRawName(ProjectConverter P, CodeWriter writer, EocStruct[] collection)
        {
            //In e::user::type::eoc_internal
            foreach (var item in collection)
            {
                item.DefineRawName(writer);
            }
        }

        public static void DefineName(ProjectConverter P, CodeWriter writer, EocStruct[] collection)
        {
            //In e::user::type
            foreach (var item in collection)
            {
                item.DefineName(writer);
            }
        }
        public static void DefineRawStructInfo(ProjectConverter P, CodeWriter writer, EocStruct[] collection)
        {
            //In e::user::type::eoc_internal
            foreach (var item in collection)
            {
                item.DefineRawStructInfo(writer);
            }
        }

        public static void DefineStructMarshaler(ProjectConverter P, CodeWriter writer, EocStruct[] collection)
        {
            //In e::system
            var map = collection.ToDictionary(x => x.RawInfo.Id);
            var graph = new AdjacencyGraph<EocStruct, IEdge<EocStruct>>();
            foreach (var item in collection)
            {
                var hasDependentItem = false;
                foreach (var member in item.RawInfo.Member)
                {
                    if (EplSystemId.GetType(member.DataType) == EplSystemId.Type_Struct
                        && map.TryGetValue(member.DataType, out var memberType))
                    {
                        graph.AddVerticesAndEdge(new Edge<EocStruct>(memberType, item));
                        hasDependentItem = true;
                    }
                }
                if (!hasDependentItem)
                {
                    graph.AddVertex(item);
                }
            }

            foreach (var item in graph.TopologicalSort())
            {
                item.DefineStructMarshaler(writer);
            }
        }

        public static EocStruct[] Translate(ProjectConverter P, IEnumerable<StructInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocStruct Translate(ProjectConverter P, StructInfo rawInfo)
        {
            return new EocStruct(P, rawInfo);
        }
    }
}
