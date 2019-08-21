using QIQI.EProjectFile;
using QuickGraph;
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

        private void ImplementItem(CodeWriter writer, Dictionary<string, string> moduleMap, Dictionary<Tuple<string, string>, string> funcMap)
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

                var funcId = funcMap[new Tuple<string, string>(moduleMap[LibraryName], EntryPoint)];
                writer.Write($"{P.DllNamespace}::eoc_func::GetFuncPtr_{funcId}()");

                writer.Write(string.Join("", paramName.Select(x => $", {x}")));
                writer.Write(");");
            }
        }

        public static EocDll[] Translate(ProjectConverter P, IEnumerable<DllDeclareInfo> dllDeclare)
        {
            return dllDeclare.Select(x => Translate(P, x)).ToArray();
        }

        public static EocDll Translate(ProjectConverter P, DllDeclareInfo dllDeclare)
        {
            var libraryName = dllDeclare.LibraryName;
            var entryPoint = dllDeclare.EntryPoint;
            if (string.IsNullOrEmpty(entryPoint))
            {
                entryPoint = P.IdToNameMap.GetUserDefinedName(dllDeclare.Id);
            }
            return new EocDll(P, P.GetUserDefinedName_SimpleCppName(dllDeclare.Id), P.GetEocCmdInfo(dllDeclare), libraryName, entryPoint);
        }

        public static void Define(ProjectConverter P, CodeWriter writer, EocDll[] eocDlls)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"type.h\"");
            using (writer.NewNamespace(P.DllNamespace))
            {
                foreach (var item in eocDlls)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, EocDll[] eocDlls)
        {
            writer.Write("#include \"dll.h\"");
            writer.NewLine();
            writer.Write("#include <e/system/dll_core.h>");
            writer.NewLine();
            writer.Write("#include <e/system/methodptr_caller.h>");
            using (writer.NewNamespace(P.DllNamespace))
            {
                var moduleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var funcMap = new Dictionary<Tuple<string, string>, string>();
                for (int i = 0, j = 0, k = 0; i < eocDlls.Length; i++)
                {
                    var item = eocDlls[i];
                    if (!moduleMap.TryGetValue(item.LibraryName, out var dllIdInCpp))
                    {
                        dllIdInCpp = (j++).ToString();
                        moduleMap.Add(item.LibraryName, dllIdInCpp);
                    }
                    var dllEntryPointPair = new Tuple<string, string>(dllIdInCpp, item.EntryPoint);
                    if (!funcMap.ContainsKey(dllEntryPointPair))
                    {
                        funcMap.Add(dllEntryPointPair, (k++).ToString());
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
                        var entryPointExpr = item.Key.Item2;
                        if (entryPointExpr.StartsWith("#")) 
                        {
                            entryPointExpr = $"reinterpret_cast<const char *>({Convert.ToInt32(entryPointExpr.Substring(1))})";
                        }
                        else
                        {
                            entryPointExpr = $"\"{entryPointExpr}\"";
                        }
                        writer.NewLine();
                        writer.Write($"eoc_DefineFuncPtrGetter({item.Value}, {P.DllNamespace}::eoc_module::GetMoudleHandle_{item.Key.Item1}(), {entryPointExpr});");
                    }
                }
                foreach (var item in eocDlls)
                {
                    item.ImplementItem(writer, moduleMap, funcMap);
                }
            }
        }
    }
}