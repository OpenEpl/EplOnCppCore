using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocConstant
    {
        public ProjectConverter P { get; }

        public EocConstant(ProjectConverter p, string name, EocConstantInfo info)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Info = info;
        }
        public string RefId => Info?.RefId;
        public string Name { get; }
        public EocConstantInfo Info { get; }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            if (RefId != null)
            {
                graph.AddVertex(RefId);
                P.AnalyzeDependencies(graph, RefId, Info.DataType);
            }
        }

        private void DefineItem(CodeWriter writer)
        {
            if (Info?.Value == null)
            {
                return;
            }
            switch (Info.Value)
            {
                case double v:
                    writer.NewLine();
                    if ((int)v == v)
                    {
                        writer.Write($"const int {Name}({v});");
                    }
                    else if ((long)v == v)
                    {
                        writer.Write($"const int64_t {Name}({v});");
                    }
                    else
                    {
                        writer.Write($"const double {Name}({v});");
                    }
                    break;

                case bool v:
                    writer.NewLine();
                    writer.Write($"const bool {Name}(" + (v ? "true" : "false") + ");");
                    break;

                case string v:
                    writer.NewLine();
                    writer.Write($"inline e::system::string {Name}()");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write("return ");
                        writer.WriteLiteral(v);
                        writer.Write(";");
                    }
                    break;

                case DateTime v:
                    writer.NewLine();
                    writer.Write($"const e::system::datetime {Name}({v.ToOADate()}/*{v.ToString("yyyyMMddTHHmmss")}*/);");
                    break;

                case byte[] v:
                    writer.NewLine();
                    writer.Write($"e::system::bin {Name}();");
                    break;
                default:
                    throw new Exception();
            }
        }

        private void ImplementItem(CodeWriter writer)
        {
            if (Info?.Value == null)
            {
                return;
            }
            switch (Info.Value)
            {
                case byte[] v:
                    writer.NewLine();
                    writer.Write($"e::system::bin {Name}()");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write("return e::system::bin {");
                        for (int i = 0; i < v.Length; i++)
                        {
                            if (i != 0)
                                writer.Write(", ");
                            writer.Write(v[i].ToString());
                        }
                        writer.Write("}");
                        writer.Write(";");
                    }
                    break;
            }
        }

        public static SortedDictionary<int, EocConstant> Translate(ProjectConverter P, IEnumerable<ConstantInfo> rawInfos)
        {
            return rawInfos.ToSortedDictionary(x => x.Id, x => Translate(P, x));
        }

        public static EocConstant Translate(ProjectConverter P, ConstantInfo rawInfo)
        {
            var name = P.GetUserDefinedName_SimpleCppName(rawInfo.Id);
            var cppName = $"{P.ConstantNamespace}::{name}";
            string getter = null;
            CppTypeName dataType;
            switch (rawInfo.Value)
            {
                case double v:
                    if ((int)v == v)
                    {
                        dataType = EocDataTypes.Int;
                    }
                    else if ((long)v == v)
                    {
                        dataType = EocDataTypes.Long;
                    }
                    else
                    {
                        dataType = EocDataTypes.Double;
                    }
                    break;

                case bool _:
                    dataType = EocDataTypes.Bool;
                    break;

                case DateTime _:
                    dataType = EocDataTypes.DateTime;
                    break;

                case string _:
                    dataType = EocDataTypes.String;
                    getter = cppName;
                    cppName = null;
                    break;

                case byte[] _:
                    dataType = EocDataTypes.Bin;
                    getter = cppName;
                    cppName = null;
                    break;

                case null:
                    return null;

                default:
                    throw new Exception();
            }

            var info = new EocConstantInfo()
            {
                CppName = cppName,
                Getter = getter,
                DataType = dataType,
                Value = rawInfo.Value
            };

            return new EocConstant(P, name, info);
        }

        public static void Define(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocConstant> map)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include <e/system/basic_type.h>");
            using (writer.NewNamespace(P.ConstantNamespace))
            {
                foreach (var item in map.Values)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocConstant> map)
        {
            writer.Write("#include \"constant.h\"");
            using (writer.NewNamespace(P.ConstantNamespace))
            {
                foreach (var item in map.Values)
                {
                    item.ImplementItem(writer);
                }
            }
        }
    }
}
