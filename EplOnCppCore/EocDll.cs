using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocDll
    {
        public ProjectConverter P { get; }
        public string RefId => Info?.CppName;

        public EocDll(ProjectConverter p, string name, EocCmdInfo info, string libraryName, string entryPoint)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            Info = info ?? throw new ArgumentNullException(nameof(info));
            LibraryName = libraryName ?? throw new ArgumentNullException(nameof(libraryName));
            EntryPoint = entryPoint ?? throw new ArgumentNullException(nameof(EntryPoint));
        }

        public string Name { get; }
        public EocCmdInfo Info { get; }
        public string LibraryName { get; }
        public string EntryPoint { get; }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            P.AnalyzeDependencies(graph, Info);
        }

        private void DefineItem(CodeWriter writer)
        {
            P.DefineMethod(writer, Info, Name, false);
        }

        private void ImplementItem(CodeWriter writer, string funcId)
        {
            var paramName = P.GetParamNameFromInfo(Info.Parameters);
            string returnTypeString = Info.ReturnDataType == null ? "void" : Info.ReturnDataType.ToString();
            string funcTypeString;
            P.WriteMethodHeader(writer, Info, Name, false, null, false);

            {
                var funcTypeStringBuilder = new StringBuilder();
                funcTypeStringBuilder.Append(returnTypeString);
                funcTypeStringBuilder.Append("(");
                for (int i = 0; i < Info.Parameters.Count; i++)
                {
                    if (i != 0)
                        funcTypeStringBuilder.Append(", ");
                    funcTypeStringBuilder.Append(P.GetParameterTypeString(Info.Parameters[i]));
                }
                funcTypeStringBuilder.Append(")");
                funcTypeString = funcTypeStringBuilder.ToString();
            }

            using (writer.NewBlock())
            {
                writer.NewLine();

                writer.Write("return e::system::MethodPtrCaller<");
                writer.Write(funcTypeString);
                writer.Write(">::call(");

                writer.Write($"{P.DllNamespace}::eoc_func::GetFuncPtr_{funcId}()");

                writer.Write(string.Join("", paramName.Select(x => $", {x}")));
                writer.Write(");");
            }
        }

        public static SortedDictionary<int, EocDll> Translate(ProjectConverter P, IEnumerable<DllDeclareInfo> rawInfos)
        {
            return rawInfos.ToSortedDictionary(x => x.Id, x => Translate(P, x));
        }

        public static EocDll Translate(ProjectConverter P, DllDeclareInfo rawInfo)
        {
            var libraryName = rawInfo.LibraryName;
            var entryPoint = rawInfo.EntryPoint;
            if (string.IsNullOrEmpty(entryPoint))
            {
                entryPoint = P.IdToNameMap.GetUserDefinedName(rawInfo.Id);
            }
            var name = P.GetUserDefinedName_SimpleCppName(rawInfo.Id);
            var info = new EocCmdInfo()
            {
                ReturnDataType = rawInfo.ReturnDataType == 0 ? null : EocDataTypes.Translate(P, rawInfo.ReturnDataType),
                CppName = $"{P.DllNamespace}::{name}",
                Parameters = rawInfo.Parameters.Select((x) =>
                {
                    var dataType = EocDataTypes.Translate(P, x.DataType, x.ArrayParameter);
                    return new EocParameterInfo()
                    {
                        ByRef = x.ByRef || x.ArrayParameter || !EocDataTypes.IsValueType(dataType),
                        Optional = false,
                        VarArgs = false,
                        DataType = dataType,
                        CppName = P.GetUserDefinedName_SimpleCppName(x.Id)
                    };
                }).ToList()
            };
            return new EocDll(P, name, info, libraryName, entryPoint);
        }

        public static void Define(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocDll> map)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"type.h\"");
            using (writer.NewNamespace(P.DllNamespace))
            {
                foreach (var item in map.Values)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, SortedDictionary<int, EocDll> map)
        {
            writer.Write("#include \"dll.h\"");
            writer.NewLine();
            writer.Write("#include <e/system/dll_core.h>");
            writer.NewLine();
            writer.Write("#include <e/system/methodptr_caller.h>");
            using (writer.NewNamespace(P.DllNamespace))
            {
                var moduleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var funcMap = new Dictionary<(string dllId, string entryPoint), string>();
                var funcToImplement = new List<(EocDll dll, string funcId)>();

                {
                    int dllIdCount = 0, funcIdCount = 0;
                    var enumerator = map.Values.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var item = enumerator.Current;
                        if (!moduleMap.TryGetValue(item.LibraryName, out var dllId))
                        {
                            dllId = (dllIdCount++).ToString();
                            moduleMap.Add(item.LibraryName, dllId);
                        }
                        var funcKey = (dllId, item.EntryPoint);
                        if (!funcMap.TryGetValue(funcKey, out var funcId))
                        {
                            funcId = (funcIdCount++).ToString();
                            funcMap.Add(funcKey, funcId);
                        }
                        funcToImplement.Add((item, funcId));
                    }
                }

                using (writer.NewNamespace("eoc_module"))
                {
                    foreach (var item in moduleMap)
                    {
                        writer.NewLine();
                        writer.Write($"eoc_DefineMoudleLoader({item.Value}, \"{item.Key}\");");
                    }
                }

                using (writer.NewNamespace("eoc_func"))
                {
                    foreach (var item in funcMap)
                    {
                        var entryPointExpr = item.Key.entryPoint;
                        if (entryPointExpr.StartsWith("#"))
                        {
                            entryPointExpr = $"reinterpret_cast<const char *>({Convert.ToInt32(entryPointExpr.Substring(1))})";
                        }
                        else
                        {
                            entryPointExpr = $"\"{entryPointExpr}\"";
                        }
                        writer.NewLine();
                        writer.Write($"eoc_DefineFuncPtrGetter({item.Value}, {P.DllNamespace}::eoc_module::GetMoudleHandle_{item.Key.dllId}(), {entryPointExpr});");
                    }
                }

                foreach (var (dll, funcId) in funcToImplement)
                {
                    dll.ImplementItem(writer, funcId);
                }
            }
        }
    }
}