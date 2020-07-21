using QIQI.EplOnCpp.Core.Utils;
using QIQI.EProjectFile;
using QIQI.EProjectFile.Expressions;
using QIQI.EProjectFile.LibInfo;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class ProjectConverter
    {
        /// <summary>
        /// 使用<code>new ProjectConverter(...).Generate(...)</code>替代
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="projectType"></param>
        /// <param name="projectNamespace"></param>
        [Obsolete]
        public static void Convert(EProjectFile.EProjectFile source, string dest, EocProjectType projectType = EocProjectType.Console, string projectNamespace = "e::user")
        {
            new ProjectConverter(source, projectType, projectNamespace).Generate(dest);
        }

        public LibInfo[] Libs { get; }
        public ReadOnlyCollection<Dictionary<int, int>> LibCmdToDeclaringTypeMap { get; }
        public EocLibInfo[] EocLibs { get; }
        public int EocHelperLibId { get; }
        public int DataTypeId_IntPtr { get; }
        public EocProjectType ProjectType { get; }
        public SortedDictionary<int, EocConstant> EocConstantMap { get; set; }
        public SortedDictionary<int, EocStruct> EocStructMap { get; set; }
        public SortedDictionary<int, EocGlobalVariable> EocGlobalVariableMap { get; set; }
        public SortedDictionary<int, EocDll> EocDllDeclareMap { get; set; }
        public SortedDictionary<int, EocObjectClass> EocObjectClassMap { get; set; }
        public SortedDictionary<int, EocStaticClass> EocStaticClassMap { get; set; }
        public SortedDictionary<int, EocDllExport> EocDllExportMap { get; set; }
        public SortedDictionary<int, EocMemberInfo> EocMemberMap { get; set; }
        public SortedDictionary<int, CodeConverter> EocMethodMap { get; set; }
        public IdToNameMap IdToNameMap { get; }
        public Dictionary<int, MethodInfo> MethodIdMap { get; }

        //MethodInfo.Class 似乎并不可靠
        public Dictionary<int, ClassInfo> MethodIdToClassMap { get; }

        public AdjacencyGraph<string, IEdge<string>> DependencyGraph { get; } = new AdjacencyGraph<string, IEdge<string>>();

        public string ProjectNamespace { get; }
        public string TypeNamespace { get; }
        public string CmdNamespace { get; }
        public string DllNamespace { get; }
        public string ConstantNamespace { get; }
        public string GlobalNamespace { get; }
        public EProjectFile.EProjectFile Source { get; }
        public ILoggerWithContext Logger { get; }
        public HashSet<string> Dependencies { get; set; }
        private List<string> SourceFiles;

        public enum EocProjectType
        {
            Windows,
            Console,
            Dll
        }

        public ProjectConverter(EProjectFile.EProjectFile source, EocProjectType projectType = EocProjectType.Console, string projectNamespace = "e::user", ILoggerWithContext logger = null)
        {
            this.Logger = logger ?? new NullLoggerWithContext();

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.IdToNameMap = new IdToNameMap(source.Code, source.Resource, source.LosableSection);
            this.MethodIdMap = source.Code.Methods.ToDictionary(x => x.Id);

            this.MethodIdToClassMap = new Dictionary<int, ClassInfo>();
            foreach (var item in source.Code.Classes)
            {
                Array.ForEach(item.Method, x => MethodIdToClassMap.Add(x, item));
            }

            projectNamespace = projectNamespace ?? "e::user";
            this.ProjectNamespace = projectNamespace;
            this.TypeNamespace = projectNamespace + "::type";
            this.CmdNamespace = projectNamespace + "::cmd";
            this.DllNamespace = projectNamespace + "::dll";
            this.ConstantNamespace = projectNamespace + "::constant";
            this.GlobalNamespace = projectNamespace + "::global";
            this.Source = source;
            this.Libs = source.Code.Libraries.Select(
                x =>
                {
                    try
                    {
                        return LibInfo.Load(x);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("加载fne信息失败，请检查易语言环境，支持库：{0}，异常信息：{1}", x.Name, ex);
                        return null;
                    }
                }).ToArray();
            this.EocLibs = source.Code.Libraries.Select(
                x =>
                {
                    try
                    {
                        return EocLibInfo.Load(x);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("加载eoc库信息失败，请检查eoc环境，支持库：{0}，异常信息：{1}", x.Name, ex);
                        return null;
                    }
                }).ToArray();
            LibCmdToDeclaringTypeMap = Libs.Select(lib => {
                var r = new Dictionary<int, int>();
                for (int i = 0; i < lib?.DataType?.Length; i++)
                {
                    if (lib.DataType[i].Method != null)
                    {
                        Array.ForEach(lib.DataType[i].Method, x => r[x] = i);
                    }
                }
                return r;
            }).ToList().AsReadOnly();

            this.EocHelperLibId = Array.FindIndex(source.Code.Libraries, x => x.FileName.ToLower() == "EocHelper".ToLower());
            this.DataTypeId_IntPtr = this.EocHelperLibId == -1 ? -1 : EplSystemId.MakeLibDataTypeId((short)this.EocHelperLibId, 0);
            this.ProjectType = projectType;

            this.EocConstantMap = EocConstant.Translate(this, Source.Resource.Constants);
            this.EocStructMap = EocStruct.Translate(this, Source.Code.Structs);
            this.EocGlobalVariableMap = EocGlobalVariable.Translate(this, Source.Code.GlobalVariables);
            this.EocDllDeclareMap = EocDll.Translate(this, Source.Code.DllDeclares);
            this.EocObjectClassMap = EocObjectClass.Translate(this, Source.Code.Classes.Where(x => EplSystemId.GetType(x.Id) == EplSystemId.Type_Class));
            this.EocStaticClassMap = EocStaticClass.Translate(this, Source.Code.Classes.Where(x => EplSystemId.GetType(x.Id) != EplSystemId.Type_Class));


            this.EocMemberMap =
                this.EocObjectClassMap.Values.SelectMany(x => x.MemberInfoMap)
                .Concat(this.EocStaticClassMap.Values.SelectMany(x => x.MemberInfoMap))
                .Concat(this.EocStructMap.Values.SelectMany(x => x.MemberInfoMap))
                .ToSortedDictionary();

            this.EocMethodMap =
                this.EocObjectClassMap.Values.SelectMany(x => x.Method)
                .Concat(this.EocStaticClassMap.Values.SelectMany(x => x.Method))
                .ToSortedDictionary();

            if (ProjectType == EocProjectType.Dll)
            {
                this.EocDllExportMap = EocDllExport.Translate(this, Source.Code.Methods.Where(x => x.IsStatic && x.Public).Select(x => x.Id));
            }
        }

        private CodeWriter NewCodeFileByCppName(string dest, string fullName, string ext)
        {
            var relativePath = string.Join("/", fullName.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)) + "." + ext;
            SourceFiles.Add(relativePath);
            return new CodeWriter(Path.Combine(dest, relativePath));
        }

        public void Generate(string dest)
        {
            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }
            Directory.CreateDirectory(dest);
            this.SourceFiles = new List<string>();

            foreach (var eocObjectClass in EocObjectClassMap.Values)
            {
                eocObjectClass.ParseCode();
            }
            foreach (var eocStaticClass in EocStaticClassMap.Values)
            {
                eocStaticClass.ParseCode();
            }

            foreach (var eocObjectClass in EocObjectClassMap.Values)
            {
                eocObjectClass.Optimize();
            }
            foreach (var eocStaticClass in EocStaticClassMap.Values)
            {
                eocStaticClass.Optimize();
            }

            //分析依赖图
            foreach (var x in EocConstantMap.Values)
            {
                x?.AnalyzeDependencies(DependencyGraph);
            }
            foreach (var x in EocStructMap.Values)
            {
                x.AnalyzeDependencies(DependencyGraph);
            }
            foreach (var x in EocGlobalVariableMap.Values)
            {
                x.AnalyzeDependencies(DependencyGraph);
            }
            foreach (var x in EocDllDeclareMap.Values)
            {
                x.AnalyzeDependencies(DependencyGraph);
            }
            foreach (var x in EocObjectClassMap.Values)
            {
                x.AnalyzeDependencies(DependencyGraph);
            }
            foreach (var x in EocStaticClassMap.Values)
            {
                x.AnalyzeDependencies(DependencyGraph);
            }
            if (EocDllExportMap != null)
            {
                foreach (var x in EocDllExportMap.Values)
                {
                    x.AnalyzeDependencies(DependencyGraph);
                }
            }
            if (Source.InitEcSectionInfo != null)
            {
                DependencyGraph.AddVerticesAndEdgeRange(Source.InitEcSectionInfo.InitMethod.Select(x => new Edge<string>("[Root]", GetCppMethodName(x))));
            }
            if (Source.Code.MainMethod != 0)
            {
                DependencyGraph.AddVerticesAndEdge(new Edge<string>("[Root]", GetCppMethodName(Source.Code.MainMethod)));
            }
            else
            {
                DependencyGraph.AddVerticesAndEdge(new Edge<string>("[Root]", "e::user::cmd::EocUser__启动子程序"));
            }

            //生成依赖列表
            this.Dependencies = new HashSet<string>();
            GraphUtils.AnalyzeDependencies(DependencyGraph, "[Root]", this.Dependencies);

            //删除未使用代码
            EocConstantMap = EocConstantMap.FilterSortedDictionary(x => Dependencies.Contains(x.Value?.RefId));
            EocStructMap = EocStructMap.FilterSortedDictionary(x => Dependencies.Contains(x.Value.RefId));
            EocGlobalVariableMap = EocGlobalVariableMap.FilterSortedDictionary(x => Dependencies.Contains(x.Value.RefId));
            EocDllDeclareMap = EocDllDeclareMap.FilterSortedDictionary(x => Dependencies.Contains(x.Value.RefId));
            EocObjectClassMap = EocObjectClassMap.FilterSortedDictionary(x => Dependencies.Contains(x.Value.RefId));
            foreach (var x in EocObjectClassMap.Values) { x.RemoveUnusedCode(Dependencies); }
            foreach (var x in EocStaticClassMap.Values) { x.RemoveUnusedCode(Dependencies); }
            EocStaticClassMap = EocStaticClassMap.FilterSortedDictionary(x => x.Value.Method.Count != 0);

            //依赖信息
            File.WriteAllText(Path.Combine(dest, "Dependencies.txt"), string.Join("\r\n", this.Dependencies), Encoding.UTF8);
            File.WriteAllBytes(Path.Combine(dest, "DependencyGraph.gv"), Encoding.UTF8.GetBytes(GraphUtils.WriteGraphvizScript(DependencyGraph, "DependencyGraph")));

            string fileName;

            //常量
            using (var writer = NewCodeFileByCppName(dest, ConstantNamespace, "h"))
                EocConstant.Define(this, writer, EocConstantMap);
            using (var writer = NewCodeFileByCppName(dest, ConstantNamespace, "cpp"))
                EocConstant.Implement(this, writer, EocConstantMap);

            //声明自定义数据类型（结构/对象类）
            using (var writer = NewCodeFileByCppName(dest, TypeNamespace, "h"))
            {
                DefineAllTypes(writer);
            }

            //实现 对象类
            foreach (var item in EocObjectClassMap.Values)
            {
                using (var writer = NewCodeFileByCppName(dest, item.CppName, "cpp"))
                    item.ImplementRawObjectClass(writer);
            }

            //静态类
            foreach (var item in EocStaticClassMap.Values)
            {
                using (var writer = NewCodeFileByCppName(dest, item.CppName, "h"))
                    item.Define(writer);
                using (var writer = NewCodeFileByCppName(dest, item.CppName, "cpp"))
                    item.Implement(writer);
            }

            //全局变量
            using (var writer = NewCodeFileByCppName(dest, GlobalNamespace, "h"))
                EocGlobalVariable.Define(this, writer, EocGlobalVariableMap);
            using (var writer = NewCodeFileByCppName(dest, GlobalNamespace, "cpp"))
                EocGlobalVariable.Implement(this, writer, EocGlobalVariableMap);

            //DLL
            using (var writer = NewCodeFileByCppName(dest, DllNamespace, "h"))
                EocDll.Define(this, writer, EocDllDeclareMap);
            using (var writer = NewCodeFileByCppName(dest, DllNamespace, "cpp"))
                EocDll.Implement(this, writer, EocDllDeclareMap);

            //预编译头
            using (var writer = NewCodeFileByCppName(dest, "stdafx", "h"))
                MakeStandardHeader(writer);

            //程序入口
            using (var writer = NewCodeFileByCppName(dest, "entry", "cpp"))
                MakeProgramEntry(writer);

            //Dll导出
            if (EocDllExportMap != null)
            {
                using (var writer = NewCodeFileByCppName(dest, "dll_export", "cpp"))
                    EocDllExport.Implement(this, writer, EocDllExportMap);
                fileName = Path.Combine(dest, "dll_export.def");
                using (var writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
                {
                    EocDllExport.MakeDef(this, writer, EocDllExportMap);
                }
            }

            //CMake项目配置文件
            fileName = Path.Combine(dest, "CMakeLists.txt");
            using (var writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
                MakeCMakeLists(writer);

            //VSCode配置文件
            fileName = Path.Combine(dest, ".vscode", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
                MakeVSCodeSettings(writer);
        }

        private void MakeStandardHeader(CodeWriter writer)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include <e/system/func.h>");
            writer.NewLine();
            writer.Write("#include \"e/user/type.h\"");
            writer.NewLine();
            writer.Write("#include \"e/user/constant.h\"");
            writer.NewLine();
            writer.Write("#include \"e/user/dll.h\"");
            writer.NewLine();
            writer.Write("#include \"e/user/global.h\"");
            foreach (var item in EocStaticClassMap.Values)
            {
                var includeName = item.CppName.Replace("::", "/") + ".h";
                writer.NewLine();
                writer.Write($"#include \"{includeName}\"");
            }
        }

        private void MakeProgramEntry(CodeWriter writer)
        {
            writer.Write("#include \"stdafx.h\"");
            writer.NewLine();
            writer.Write("#include <Windows.h>");
            writer.NewLine();
            writer.Write("int init()");
            using (writer.NewBlock())
            {
                if (Source.InitEcSectionInfo != null)
                {
                    for (int i = 0; i < Source.InitEcSectionInfo.InitMethod.Length; i++)
                    {
                        writer.NewLine();
                        writer.Write(GetCppMethodName(Source.InitEcSectionInfo.InitMethod[i]));
                        writer.Write("();");
                        writer.AddComment("为{" + Source.InitEcSectionInfo.EcName[i] + "}做初始化");
                    }
                }
                if (Source.Code.MainMethod != 0)
                {
                    writer.NewLine();
                    writer.Write("return ");
                    writer.Write(GetCppMethodName(Source.Code.MainMethod));
                    writer.Write("();");
                }
                else
                {
                    writer.NewLine();
                    writer.Write("return e::user::cmd::EocUser__启动子程序();");
                }
            }
            switch (ProjectType)
            {
                case EocProjectType.Windows:
                    writer.NewLine();
                    writer.Write("int WINAPI WinMain(HINSTANCE hInstance,HINSTANCE hPrevInstance,PSTR szCmdLine, int iCmdShow)");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write("return init();");
                    }
                    break;

                case EocProjectType.Console:
                    writer.NewLine();
                    writer.Write("int main()");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write("return init();");
                    }
                    break;

                case EocProjectType.Dll:
                    writer.NewLine();
                    writer.Write("BOOL APIENTRY DllMain(HANDLE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)");
                    using (writer.NewBlock())
                    {
                        writer.NewLine();
                        writer.Write("switch(ul_reason_for_call)");
                        using (writer.NewBlock())
                        {
                            writer.NewLine();
                            writer.Write("case DLL_PROCESS_ATTACH:");
                            writer.NewLine();
                            writer.Write("init();");
                            writer.NewLine();
                            writer.Write("break;");
                        }
                        writer.NewLine();
                        writer.Write("return TRUE;");
                    }
                    break;

                default:
                    throw new Exception("未知项目类型");
            }
        }

        private void MakeCMakeLists(StreamWriter writer)
        {
            //请求CMake
            writer.WriteLine("cmake_minimum_required(VERSION 3.8)");
            writer.WriteLine();
            //引用EocBuildHelper
            writer.WriteLine("if(NOT DEFINED EOC_HOME)");
            writer.WriteLine("    set(EOC_HOME $ENV{EOC_HOME})");
            writer.WriteLine("endif()");
            writer.WriteLine("include(${EOC_HOME}/EocBuildHelper.cmake)");
            writer.WriteLine();
            //建立项目
            writer.WriteLine("project(main)");
            switch (ProjectType)
            {
                case EocProjectType.Windows:
                    writer.WriteLine("add_executable(main WIN32)");
                    break;

                case EocProjectType.Console:
                    writer.WriteLine("add_executable(main)");
                    break;

                case EocProjectType.Dll:
                    writer.WriteLine("add_library(main SHARED dll_export.def)");
                    break;

                default:
                    throw new Exception("未知项目类型");
            }
            writer.WriteLine();
            //添加源代码
            writer.Write("target_sources(main PRIVATE ");
            foreach (var src in SourceFiles)
            {
                writer.WriteLine();
                writer.Write("                            ");
                writer.Write("\"");
                writer.Write(src);
                writer.Write("\"");
            }
            writer.WriteLine(")");
            writer.WriteLine();
            //启用C++17
            writer.WriteLine("set_property(TARGET main PROPERTY CXX_STANDARD 17)");
            writer.WriteLine("set_property(TARGET main PROPERTY CXX_STANDARD_REQUIRED ON)");
            writer.WriteLine();
            //系统库
            writer.WriteLine("target_link_eoc_lib(main system EocSystem)");
            //支持库
            for (int i = 0; i < Source.Code.Libraries.Length; i++)
            {
                LibraryRefInfo item = Source.Code.Libraries[i];
                string libCMakeName = EocLibs[i]?.CMakeName;
                if (string.IsNullOrEmpty(libCMakeName))
                    continue;
                writer.WriteLine($"target_link_eoc_lib(main {item.FileName} {libCMakeName})");
            }
        }

        private static void MakeVSCodeSettings(StreamWriter writer)
        {
            writer.WriteLine("{");
            writer.WriteLine("    \"C_Cpp.default.configurationProvider\": \"vector-of-bool.cmake-tools\",");
            writer.WriteLine("    \"[cpp]\": {");
            writer.WriteLine("        \"files.encoding\": \"utf8bom\"");
            writer.WriteLine("    },");
            writer.WriteLine("    \"files.exclude\": {");
            writer.WriteLine("        \".vs\": true, ");
            writer.WriteLine("        \"build\": true, ");
            writer.WriteLine("        \"out\": true");
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }

        private void DefineAllTypes(CodeWriter writer)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include <e/system/basic_type.h>");
            ReferenceEocLibs(writer);
            using (writer.NewNamespace(TypeNamespace))
            {
                using (writer.NewNamespace("eoc_internal"))
                {
                    EocStruct.DefineRawName(this, writer, EocStructMap);
                    EocObjectClass.DefineRawName(this, writer, EocObjectClassMap);
                }
                EocStruct.DefineName(this, writer, EocStructMap);
                EocObjectClass.DefineName(this, writer, EocObjectClassMap);
                using (writer.NewNamespace("eoc_internal"))
                {
                    EocStruct.DefineRawStructInfo(this, writer, EocStructMap);
                    EocObjectClass.DefineRawObjectClass(this, writer, EocObjectClassMap);
                }
            }
            using (writer.NewNamespace("e::system"))
            {
                EocStruct.DefineStructMarshaler(this, writer, EocStructMap);
            }
        }

        private void ReferenceEocLibs(CodeWriter writer)
        {
            for (int i = 0; i < Source.Code.Libraries.Length; i++)
            {
                if (EocLibs[i] == null)
                    continue;
                LibraryRefInfo item = Source.Code.Libraries[i];
                writer.NewLine();
                writer.Write($"#include <e/lib/{item.FileName}/public.h>");
            }
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph, string a, CppTypeName b)
        {
            if (b == null)
                return;
            graph.AddVerticesAndEdge(new Edge<string>(a, b.Name));
            if (b.TypeParam == null)
                return;
            foreach (var item in b.TypeParam)
            {
                AnalyzeDependencies(graph, a, item);
            }
        }
        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph, EocCmdInfo info, string refId = null)
        {
            if (info == null)
                return;
            refId = refId ?? info.CppName;
            graph.AddVertex(refId);
            AnalyzeDependencies(graph, refId, info.ReturnDataType);
            foreach (var x in info.Parameters)
            {
                var varRefId = $"{refId}|{x.CppName}";
                graph.AddVerticesAndEdge(new Edge<string>(refId, varRefId));
                AnalyzeDependencies(graph, varRefId, x.DataType);
            }
        }

        internal void InitMembersInConstructor(CodeWriter writer, IEnumerable<EocVariableInfo> collection)
        {
            bool first = true;
            foreach (var item in collection)
            {
                if (first)
                    first = false;
                else
                    writer.Write(", ");
                writer.Write(item.CppName);
                writer.Write("(");
                writer.Write(EocDataTypes.GetInitParameter(item.DataType, item.UBound));
                writer.Write(")");
            }
        }

        internal void DefineVariable(CodeWriter writer, string[] modifiers, EocVariableInfo variable, bool initAtOnce = true)
        {
            writer.NewLine();
            if (modifiers != null)
            {
                foreach (var item in modifiers)
                {
                    writer.Write(item);
                    writer.Write(" ");
                }
            }
            writer.Write(variable.DataType.ToString());
            writer.Write(" ");
            writer.Write(variable.CppName.Split(new string[] { "::" }, StringSplitOptions.None).LastOrDefault());
            if (initAtOnce)
            {
                var initParameter = EocDataTypes.GetInitParameter(variable.DataType, variable.UBound);
                if (!string.IsNullOrWhiteSpace(initParameter))
                {
                    writer.Write("(");
                    writer.Write(initParameter);
                    writer.Write(")");
                }
            }
            writer.Write(";");
        }

        internal void DefineVariable(CodeWriter writer, string[] modifiers, IEnumerable<EocVariableInfo> collection, bool initAtOnce = true)
        {
            foreach (var item in collection)
            {
                DefineVariable(writer, modifiers, item, initAtOnce);
            }
        }

        internal void DefineMethod(CodeWriter writer, EocCmdInfo eocCmdInfo, string name, bool isVirtual)
        {
            WriteMethodHeader(writer, eocCmdInfo, name, isVirtual, null, true);
            writer.Write(";");
        }

        public string[] GetParamNameFromInfo(List<EocParameterInfo> infos)
        {
            string[] result = new string[infos.Count];
            for (int i = 0; i < infos.Count; i++)
            {
                result[i] = infos[i].CppName ?? $"_AnonymousParameter{i + 1}";
            }
            return result;
        }

        internal void WriteMethodHeader(CodeWriter writer, EocCmdInfo eocCmdInfo, string name, bool isVirtual, string className = null, bool writeDefaultValue = true)
        {
            var paramNames = GetParamNameFromInfo(eocCmdInfo.Parameters);
            var numOfAuto = 0;
            for (int i = 0; i < eocCmdInfo.Parameters.Count; i++)
            {
                if (numOfAuto == 0)
                {
                    writer.NewLine();
                    writer.Write("template <");
                }
                else
                {
                    writer.Write(", ");
                }
                writer.Write($"typename _EocAutoParam_{paramNames[i]}");
                numOfAuto++;
            }
            if (numOfAuto != 0)
            {
                writer.Write(">");
            }

            writer.NewLine();
            if (isVirtual)
            {
                writer.Write("virtual ");
            }

            var numOfOptionalAtEnd = 0;
            for (int i = eocCmdInfo.Parameters.Count - 1; i >= 0; i--)
            {
                if (eocCmdInfo.Parameters[i].Optional)
                    numOfOptionalAtEnd++;
                else
                    break;
            }
            var startOfOptionalAtEnd = eocCmdInfo.Parameters.Count - numOfOptionalAtEnd;

            writer.Write(eocCmdInfo.ReturnDataType == null ? "void" : eocCmdInfo.ReturnDataType.ToString());
            writer.Write(" __stdcall ");
            if (className != null)
            {
                writer.Write(className);
                writer.Write("::");
            }
            writer.Write(name);
            writer.Write("(");
            for (int i = 0; i < eocCmdInfo.Parameters.Count; i++)
            {
                if (i != 0)
                    writer.Write(", ");
                writer.Write(GetParameterTypeString(eocCmdInfo.Parameters[i], $"_EocAutoParam_{paramNames[i]}"));
                writer.Write(" ");
                writer.Write(paramNames[i]);
                if (writeDefaultValue && i >= startOfOptionalAtEnd)
                {
                    writer.Write(" = std::nullopt");
                }
            }
            writer.Write(")");
        }

        public int CalculateArraySize(int[] UBound)
        {
            if (UBound == null || UBound.Length == 0)
            {
                return 0;
            }
            int size = 1;
            foreach (var item in UBound)
            {
                size *= item;
            }
            return size;
        }

        public string GetUserDefinedName_SimpleCppName(int id)
        {
            return "EocUser_" + IdToNameMap.GetUserDefinedName(id);
        }

        #region MethodInfoHelper

        public string GetCppMethodName(int id)
        {
            switch (EplSystemId.GetType(id))
            {
                case EplSystemId.Type_Method:
                    if (EplSystemId.GetType(MethodIdToClassMap[id].Id) == EplSystemId.Type_Class)
                    {
                        return GetUserDefinedName_SimpleCppName(id);
                    }
                    else
                    {
                        return CmdNamespace + "::" + GetUserDefinedName_SimpleCppName(id);
                    }
                case EplSystemId.Type_Dll:
                    return DllNamespace + "::" + GetUserDefinedName_SimpleCppName(id);

                default:
                    throw new Exception();
            }
        }

        public EocMemberInfo GetEocMemberInfo(AccessMemberExpression expr)
        {
            return GetEocMemberInfo(expr.LibraryId, expr.StructId, expr.MemberId);
        }

        public EocMemberInfo GetEocMemberInfo(int libId, int structId, int id)
        {
            switch (libId)
            {
                case -2:
                    return GetEocMemberInfo(structId, id);

                default:
                    var dataTypeInfo = Libs[libId].DataType[structId];
                    var memberInfo = dataTypeInfo.Member[id];
                    var eocMemberInfo = EocLibs[libId].Type[dataTypeInfo.Name].Member[memberInfo.Name];
                    return eocMemberInfo;
            }
        }

        public EocMemberInfo GetEocMemberInfo(int structId, int id)
        {
            return EocMemberMap[id];
        }

        public EocConstantInfo GetEocConstantInfo(EmnuConstantExpression expr)
        {
            var dataTypeInfo = Libs[expr.LibraryId].DataType[expr.StructId];
            var memberInfo = dataTypeInfo.Member[expr.MemberId];
            EocConstantInfo result;
            try
            {
                result = EocLibs[expr.LibraryId].Enum[dataTypeInfo.Name][memberInfo.Name];
            }
            catch (Exception)
            {
                result = new EocConstantInfo()
                {
                    Value = memberInfo.Default
                };
                if (result.Value is long longValue)
                    if ((int)longValue == longValue)
                        result.Value = (int)longValue;
                result.DataType = EocDataTypes.GetConstValueType(result.Value);
            }
            return result;
        }

        public EocConstantInfo GetEocConstantInfo(ConstantExpression expr)
        {
            return GetEocConstantInfo(expr.LibraryId, expr.ConstantId);
        }

        public EocConstantInfo GetEocConstantInfo(int libraryId, int id)
        {
            switch (libraryId)
            {
                case -2:
                    return GetEocConstantInfo(id);

                default:
                    var name = Libs[libraryId].Constant[id].Name;
                    EocConstantInfo result;
                    try
                    {
                        result = EocLibs[libraryId].Constant[name];
                    }
                    catch (Exception)
                    {
                        result = new EocConstantInfo()
                        {
                            Value = Libs[libraryId].Constant[id].Value
                        };
                        if (result.Value is long longValue)
                            if ((int)longValue == longValue)
                                result.Value = (int)longValue;
                        result.DataType = EocDataTypes.GetConstValueType(result.Value);
                    }
                    return result;
            }
        }

        public EocConstantInfo GetEocConstantInfo(int id)
        {
            return EocConstantMap[id].Info;
        }

        public EocCmdInfo GetEocCmdInfo(CallExpression expr)
        {
            return GetEocCmdInfo(expr.LibraryId, expr.MethodId);
        }

        public EocCmdInfo GetEocCmdInfo(MethodPtrExpression expr)
        {
            return GetEocCmdInfo(expr.MethodId);
        }

        public EocCmdInfo GetEocCmdInfo(int libId, int id)
        {
            switch (libId)
            {
                case -2:
                case -3:
                    return GetEocCmdInfo(id);

                default:
                    EocCmdInfo result;
                    if (Libs[libId] == null)
                    {
                        throw new Exception($"缺少fne信息：{Source.Code.Libraries[libId].Name}");
                    }
                    if (EocLibs[libId] == null)
                    {
                        throw new Exception($"{Libs[libId].Name} 库缺少Eoc识别信息，可能是Eoc不支持该库或没有安装相应Eoc库");
                    }
                    if (Libs[libId].Cmd.Length < id)
                    {
                        throw new Exception($"fne信息中缺少命令信息，请检查版本是否匹配【Lib：{Libs[libId].Name}，CmdId：{id}】");
                    }
                    var name = Libs[libId].Cmd[id].Name;
                    if (LibCmdToDeclaringTypeMap[libId].TryGetValue(id, out var typeId))
                    {
                        var typeName = Libs[libId].DataType[typeId].Name;
                        if (!EocLibs[libId].Type.TryGetValue(typeName, out var typeInfo))
                        {
                            throw new Exception($"{typeName} 类型缺少Eoc识别信息，可能是Eoc不支持该类型或没有安装相应Eoc库");
                        }
                        if (!typeInfo.Method.TryGetValue(name, out result))
                        {
                            throw new Exception($"{typeName}.{name} 命令缺少Eoc识别信息，可能是Eoc不支持该命令或没有安装相应Eoc库");
                        }
                    }
                    else
                    {
                        if (!EocLibs[libId].Cmd.TryGetValue(name, out result))
                        {
                            throw new Exception($"{name} 命令缺少Eoc识别信息，可能是Eoc不支持该命令或没有安装相应Eoc库");
                        }
                        return EocLibs[libId].Cmd[name];
                    }
                    return result;
            }
        }

        public EocCmdInfo GetEocCmdInfo(int id)
        {
            switch (EplSystemId.GetType(id))
            {
                case EplSystemId.Type_Method:
                    return EocMethodMap[id].Info;

                case EplSystemId.Type_Dll:
                    return EocDllDeclareMap[id].Info;

                default:
                    throw new Exception();
            }
        }

        public string GetParameterTypeString(EocParameterInfo x, string typeForAuto = null)
        {
            var r = x.DataType.ToString();
            if (!string.IsNullOrEmpty(typeForAuto) && x.DataType == EocDataTypes.Auto)
            {
                r = typeForAuto;
            }
            if (x.Optional)
            {
                if (x.ByRef)
                    r = $"std::optional<std::reference_wrapper<{r}>>";
                else
                    r = $"std::optional<{r}>";
            }
            else if (x.ByRef)
            {
                r = $"{r}&";
            }
            return r;
        }

        #endregion MethodInfoHelper
    }
}