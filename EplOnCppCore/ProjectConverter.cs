using QIQI.EProjectFile;
using QIQI.EProjectFile.Expressions;
using QIQI.EProjectFile.LibInfo;
using QuickGraph;
using QuickGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class ProjectConverter
    {
        public readonly static CppTypeName CppTypeName_Bin = new CppTypeName(false, "e::system::bin");
        public readonly static CppTypeName CppTypeName_Bool = new CppTypeName(false, "bool");
        public readonly static CppTypeName CppTypeName_Byte = new CppTypeName(false, "uint8_t");
        public readonly static CppTypeName CppTypeName_DateTime = new CppTypeName(false, "e::system::datetime");
        public readonly static CppTypeName CppTypeName_Double = new CppTypeName(false, "double");
        public readonly static CppTypeName CppTypeName_Float = new CppTypeName(false, "float");
        public readonly static CppTypeName CppTypeName_Int = new CppTypeName(false, "int32_t");
        public readonly static CppTypeName CppTypeName_Long = new CppTypeName(false, "int64_t");
        public readonly static CppTypeName CppTypeName_Short = new CppTypeName(false, "int16_t");
        public readonly static CppTypeName CppTypeName_IntPtr = new CppTypeName(false, "intptr_t");
        public readonly static CppTypeName CppTypeName_MethodPtr = new CppTypeName(false, "e::system::methodptr");
        public readonly static CppTypeName CppTypeName_String = new CppTypeName(false, "e::system::string");
        public readonly static CppTypeName CppTypeName_Any = new CppTypeName(false, "e::system::any");
        public readonly static CppTypeName CppTypeName_SkipCheck = CppTypeName.Parse("*");

        public static readonly Dictionary<Type, CppTypeName> ConstTypeMap = new Dictionary<Type, CppTypeName>()
        {
            { typeof(byte), CppTypeName_Byte },
            { typeof(short), CppTypeName_Short },
            { typeof(int), CppTypeName_Int },
            { typeof(long), CppTypeName_Long },
            { typeof(float), CppTypeName_Float },
            { typeof(double), CppTypeName_Double },
            { typeof(IntPtr), CppTypeName_IntPtr },
            { typeof(DateTime), CppTypeName_DateTime },
            { typeof(string), CppTypeName_String },
            { typeof(bool), CppTypeName_Bool }
        };

        internal static CppTypeName GetConstValueType(object value)
        {
            var type = value.GetType();
            var isArray = type.IsArray;
            while (type.IsArray)
            {
                type = type.GetElementType();
            }

            return isArray
                ? new CppTypeName(false, "e::system::array", new[] { ConstTypeMap[type] })
                : ConstTypeMap[type];
        }

        public static readonly Dictionary<int, CppTypeName> BasicCppTypeNameMap = new Dictionary<int, CppTypeName> {
            { EplSystemId.DataType_Bin , CppTypeName_Bin },
            { EplSystemId.DataType_Bool , CppTypeName_Bool },
            { EplSystemId.DataType_Byte , CppTypeName_Byte },
            { EplSystemId.DataType_DateTime , CppTypeName_DateTime },
            { EplSystemId.DataType_Double , CppTypeName_Double },
            { EplSystemId.DataType_Float , CppTypeName_Float },
            { EplSystemId.DataType_Int , CppTypeName_Int },
            { EplSystemId.DataType_Long , CppTypeName_Long },
            { EplSystemId.DataType_Short , CppTypeName_Short },
            { EplSystemId.DataType_MethodPtr , CppTypeName_MethodPtr },
            { EplSystemId.DataType_String , CppTypeName_String }
        };

        public static readonly EocCmdInfo ErrorEocCmdInfo = new EocCmdInfo()
        {
            ReturnDataType = CppTypeName_SkipCheck,
            CppName = "EOC_ERROR_CMD",
            Parameters = new List<EocParameterInfo>() {
                new EocParameterInfo() {
                    DataType = CppTypeName_SkipCheck,
                    VarArgs = true,
                    Optional = true
                }
            }
        };

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
        public EocLibInfo[] EocLibs { get; }
        public int EocHelperLibId { get; }
        public int DataTypeId_IntPtr { get; }
        public EocProjectType ProjectType { get; }
        public IdToNameMap IdToNameMap { get; }
        public Dictionary<int, ClassInfo> ClassIdMap { get; }
        public Dictionary<int, MethodInfo> MethodIdMap { get; }
        public Dictionary<int, DllDeclareInfo> DllIdMap { get; }
        public Dictionary<int, StructInfo> StructIdMap { get; }
        public Dictionary<int, GlobalVariableInfo> GlobalVarIdMap { get; }
        public Dictionary<int, ConstantInfo> ConstantIdMap { get; }
        public Dictionary<int, ClassVariableInfo> ClassVarIdMap { get; }

        //MethodInfo.Class 似乎并不可靠
        public Dictionary<int, ClassInfo> MethodIdToClassMap { get; }

        public string ProjectNamespace { get; }
        public string TypeNamespace { get; }
        public string CmdNamespace { get; }
        public string DllNamespace { get; }
        public string ConstantNamespace { get; }
        public string GlobalNamespace { get; }
        public EProjectFile.EProjectFile Source { get; }
        public ILoggerWithContext Logger { get; }

        public enum EocProjectType
        {
            Windows,
            Console
        }

        public ProjectConverter(EProjectFile.EProjectFile source, EocProjectType projectType = EocProjectType.Console, string projectNamespace = "e::user", ILoggerWithContext logger = null)
        {
            this.Logger = logger ?? new NullLoggerWithContext();

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.IdToNameMap = new IdToNameMap(source.Code, source.Resource, source.LosableSection);
            this.ClassIdMap = source.Code.Classes.ToDictionary(x => x.Id);
            this.MethodIdMap = source.Code.Methods.ToDictionary(x => x.Id);
            this.DllIdMap = source.Code.DllDeclares.ToDictionary(x => x.Id);
            this.StructIdMap = source.Code.Structs.ToDictionary(x => x.Id);
            this.GlobalVarIdMap = source.Code.GlobalVariables.ToDictionary(x => x.Id);
            this.ConstantIdMap = source.Resource.Constants.ToDictionary(x => x.Id);

            this.ClassVarIdMap = new Dictionary<int, ClassVariableInfo>();
            this.MethodIdToClassMap = new Dictionary<int, ClassInfo>();
            foreach (var item in source.Code.Classes)
            {
                Array.ForEach(item.Method, x => MethodIdToClassMap.Add(x, item));
                Array.ForEach(item.Variables, x => ClassVarIdMap.Add(x.Id, x));
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
            this.EocHelperLibId = Array.FindIndex(source.Code.Libraries, x => x.FileName.ToLower() == "EocHelper".ToLower());
            this.DataTypeId_IntPtr = this.EocHelperLibId == -1 ? -1 : EplSystemId.MakeLibDataTypeId((short)this.EocHelperLibId, 0);
            this.ProjectType = projectType;
        }

        public void Generate(string dest)
        {
            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            string fileName;
            string curNamespace;

            curNamespace = ConstantNamespace;
            EocConstant[] eocConstants = EocConstant.Translate(this, Source.Resource.Constants);
            fileName = GetFileNameByNamespace(dest, curNamespace, "h");
            using (var writer = new CodeWriter(fileName))
            {
                EocConstant.Define(this, writer, eocConstants);
            }

            curNamespace = TypeNamespace;
            fileName = GetFileNameByNamespace(dest, curNamespace, "h");
            using (var writer = new CodeWriter(fileName))
            {
                writer.Write("#pragma once");
                writer.NewLine();
                writer.Write("#include <e/system/basic_type.h>");
                for (int i = 0; i < Source.Code.Libraries.Length; i++)
                {
                    if (EocLibs[i] == null)
                        continue;
                    LibraryRefInfo item = Source.Code.Libraries[i];
                    writer.NewLine();
                    writer.Write($"#include <e/lib/{item.FileName}/public.h>");
                }
                using (writer.NewNamespace(curNamespace))
                {
                    DefineStructNames(writer, Source.Code.Structs);
                    DefineObjectClassNames(writer, Source.Code.Classes);
                    using (writer.NewNamespace("eoc_internal"))
                    {
                        foreach (var item in Source.Code.Structs)
                        {
                            DefineInternalStructInfo(writer, item);
                        }
                        DefineInternalObjectClassInfo(writer, Source.Code.Classes);
                    }
                }
                using (writer.NewNamespace("e::system"))
                {
                    DefineStructMarshaler(writer, Source.Code.Structs);
                }
            }

            curNamespace = CmdNamespace;
            foreach (var classItem in Source.Code.Classes)
            {
                if (EplSystemId.GetType(classItem.Id) == EplSystemId.Type_Class)
                    continue;
                fileName = GetFileNameByNamespace(dest, curNamespace + "::" + IdToNameMap.GetUserDefinedName(classItem.Id), "h");
                using (var writer = new CodeWriter(fileName))
                {
                    writer.Write("#pragma once");
                    writer.NewLine();
                    writer.Write("#include \"../type.h\"");
                    using (writer.NewNamespace(curNamespace))
                    {
                        DefineStaticClassInfo(writer, classItem);
                    }
                }
            }
            foreach (var classItem in Source.Code.Classes)
            {
                if (EplSystemId.GetType(classItem.Id) == EplSystemId.Type_Class)
                    continue;
                fileName = GetFileNameByNamespace(dest, curNamespace + "::" + IdToNameMap.GetUserDefinedName(classItem.Id), "cpp");
                using (var writer = new CodeWriter(fileName))
                {
                    writer.Write("#pragma once");
                    writer.NewLine();
                    writer.Write("#include \"../../../stdafx.h\"");
                    using (writer.NewNamespace(curNamespace))
                    {
                        ImplementStaticClass(writer, classItem);
                    }
                }
            }

            curNamespace = TypeNamespace;
            foreach (var classItem in Source.Code.Classes)
            {
                if (EplSystemId.GetType(classItem.Id) != EplSystemId.Type_Class)
                    continue;
                fileName = GetFileNameByNamespace(dest, curNamespace + "::" + IdToNameMap.GetUserDefinedName(classItem.Id), "cpp");
                using (var writer = new CodeWriter(fileName))
                {
                    writer.Write("#pragma once");
                    writer.NewLine();
                    writer.Write("#include \"../../../stdafx.h\"");
                    using (writer.NewNamespace(curNamespace))
                    {
                        ImplementObjectClass(writer, classItem);
                    }
                }
            }

            curNamespace = GlobalNamespace;
            fileName = GetFileNameByNamespace(dest, curNamespace, "h");
            using (var writer = new CodeWriter(fileName))
            {
                writer.Write("#pragma once");
                writer.NewLine();
                writer.Write("#include \"type.h\"");
                using (writer.NewNamespace(curNamespace))
                {
                    DefineVariable(writer, new string[] { "extern" }, Source.Code.GlobalVariables, false);
                }
            }
            fileName = GetFileNameByNamespace(dest, curNamespace, "cpp");
            using (var writer = new CodeWriter(fileName))
            {
                writer.Write("#pragma once");
                writer.NewLine();
                writer.Write("#include \"../../stdafx.h\"");
                using (writer.NewNamespace(curNamespace))
                {
                    DefineVariable(writer, null, Source.Code.GlobalVariables);
                }
            }

            curNamespace = DllNamespace;
            EocDll[] eocDllDeclares = EocDll.Translate(this, Source.Code.DllDeclares);
            fileName = GetFileNameByNamespace(dest, curNamespace, "h");
            using (var writer = new CodeWriter(fileName))
            {
                EocDll.Define(this, writer, eocDllDeclares);
            }
            fileName = GetFileNameByNamespace(dest, curNamespace, "cpp");
            using (var writer = new CodeWriter(fileName))
            {
                EocDll.Implement(this, writer, eocDllDeclares);
            }

            fileName = GetFileNameByNamespace(dest, "stdafx", "h");
            using (var writer = new CodeWriter(fileName))
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
                foreach (var classItem in Source.Code.Classes)
                {
                    if (EplSystemId.GetType(classItem.Id) == EplSystemId.Type_Class)
                        continue;
                    fileName = CmdNamespace.Replace("::", "/") + "/" + IdToNameMap.GetUserDefinedName(classItem.Id) + ".h";
                    writer.NewLine();
                    writer.Write($"#include \"{fileName}\"");
                }
            }

            fileName = GetFileNameByNamespace(dest, "main", "cpp");
            using (var writer = new CodeWriter(fileName))
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
                        writer.Write("return e::user::cmd::_启动子程序();");
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

                    default:
                        throw new Exception("未知项目类型");
                }
            }

            fileName = Path.Combine(dest, "CMakeLists.txt");
            using (var writer = new StreamWriter(File.Create(fileName), Encoding.Default))
            {
                //请求CMake
                writer.WriteLine("cmake_minimum_required(VERSION 3.8)");
                writer.WriteLine();
                //引用EocBuildHelper
                writer.WriteLine("if(NOT DEFINED EOC_HOME)");
                writer.WriteLine("set(EOC_HOME $ENV{EOC_HOME})");
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

                    default:
                        throw new Exception("未知项目类型");
                }
                writer.WriteLine();
                //添加源代码
                writer.WriteLine("aux_source_directory(. DIR_SRCS_ENTRY)");
                writer.WriteLine("aux_source_directory(e/user DIR_SRCS_ROOT)");
                writer.WriteLine("aux_source_directory(e/user/cmd DIR_SRCS_CMD)");
                writer.WriteLine("aux_source_directory(e/user/type DIR_SRCS_TYPE)");
                writer.WriteLine("target_sources(main PRIVATE ${DIR_SRCS_ENTRY})");
                writer.WriteLine("target_sources(main PRIVATE ${DIR_SRCS_ROOT})");
                writer.WriteLine("target_sources(main PRIVATE ${DIR_SRCS_CMD})");
                writer.WriteLine("target_sources(main PRIVATE ${DIR_SRCS_TYPE})");
                writer.WriteLine();
                //启用C++17
                writer.WriteLine("set_property(TARGET main PROPERTY CXX_STANDARD 17)");
                writer.WriteLine("set_property(TARGET main PROPERTY CXX_STANDARD_REQUIRED ON)");
                writer.WriteLine();
                //系统库
                writer.WriteLine("include(${EOC_LIBS_DIRS}/system/config.cmake)");
                writer.WriteLine("target_include_directories(main PRIVATE ${EocSystem_INCLUDE_DIRS})");
                writer.WriteLine("target_link_libraries(main ${EocSystem_LIBRARIES})");
                writer.WriteLine();
                //支持库
                for (int i = 0; i < Source.Code.Libraries.Length; i++)
                {
                    LibraryRefInfo item = Source.Code.Libraries[i];
                    string libCMakeName = EocLibs[i]?.CMakeName;
                    if (string.IsNullOrEmpty(libCMakeName))
                        continue;
                    writer.WriteLine($"include(${{EOC_LIBS_DIRS}}/{item.FileName}/config.cmake)");
                    writer.WriteLine($"target_include_directories(main PRIVATE ${{{libCMakeName}_INCLUDE_DIRS}})");
                    writer.WriteLine($"target_link_libraries(main ${{{libCMakeName}_LIBRARIES}})");
                    writer.WriteLine();
                }
            }
        }

        private void DefineStructMarshaler(CodeWriter writer, IEnumerable<StructInfo> collection)
        {
            var graph = new AdjacencyGraph<StructInfo, IEdge<StructInfo>>();
            foreach (var item in collection)
            {
                var hasDependentItem = false;
                foreach (var member in item.Member)
                {
                    if (EplSystemId.GetType(member.DataType) == EplSystemId.Type_Struct
                        && StructIdMap.TryGetValue(member.DataType, out var memberType))
                    {
                        graph.AddVerticesAndEdge(new Edge<StructInfo>(memberType, item));
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
                DefineStructMarshaler(writer, item);
            }
        }

        private void DefineStructMarshaler(CodeWriter writer, StructInfo item)
        {
            var cppTypeString = GetCppTypeName(item.Id).ToString();
            writer.NewLine();
            writer.Write("template<> struct marshaler<");
            writer.Write(cppTypeString);
            writer.Write(">");
            using (writer.NewBlock())
            {
                writer.NewLine();
                writer.Write("private: ");

                writer.NewLine();
                writer.Write("using ManagedType = ");
                writer.Write(cppTypeString);
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
                WriteStructMarshalerCodeBlock(writer, item, "DefineMember");
                writer.Write(";");

                writer.NewLine();
                writer.Write("#pragma pack(pop)");

                writer.NewLine();
                writer.Write("static void marshal(NativeType &v, ManagedType &r)");
                WriteStructMarshalerCodeBlock(writer, item, "MarshalMember");
                writer.Write(";");

                writer.NewLine();
                writer.Write("static void cleanup(NativeType &v, ManagedType &r)");
                WriteStructMarshalerCodeBlock(writer, item, "CleanupMember");
                writer.Write(";");
            }
            writer.Write(";");
        }

        private void WriteStructMarshalerCodeBlock(CodeWriter writer, StructInfo item, string cmd)
        {
            using (writer.NewBlock())
            {
                foreach (var member in item.Member)
                {
                    var memberCppName = GetUserDefinedName_SimpleCppName(member.Id);
                    writer.NewLine();
                    if (member.ByRef)
                        writer.Write($"StructMarshaler_{cmd}_Ref(ManagedType, {memberCppName});");
                    else if (member.UBound != null && member.UBound.Length != 0)
                        writer.Write($"StructMarshaler_{cmd}_Array(ManagedType, {memberCppName}, {CalculateArraySize(member.UBound)});");
                    else
                        writer.Write($"StructMarshaler_{cmd}(ManagedType, {memberCppName});");
                }
            }
        }

        private void DefineObjectClassNames(CodeWriter writer, IEnumerable<ClassInfo> collection)
        {
            using (writer.NewNamespace("eoc_internal"))
            {
                foreach (var item in collection)
                {
                    if (EplSystemId.GetType(item.Id) != EplSystemId.Type_Class)
                        continue;
                    var name = GetUserDefinedName_SimpleCppName(item.Id);
                    var rawName = "raw_" + name;
                    writer.NewLine();
                    writer.Write($"class {rawName};");
                }
            }
            foreach (var item in collection)
            {
                if (EplSystemId.GetType(item.Id) != EplSystemId.Type_Class)
                    continue;
                var name = GetUserDefinedName_SimpleCppName(item.Id);
                var rawName = "raw_" + name;
                writer.NewLine();
                writer.Write($"typedef e::system::object_ptr<{TypeNamespace}::eoc_internal::{rawName}> {name};");
            }
        }

        private void DefineStaticClassInfo(CodeWriter writer, ClassInfo classItem)
        {
            foreach (var item in classItem.Method.Select(x => MethodIdMap[x]))
            {
                DefineMethod(writer, classItem, item);
            }
        }

        private void DefineInternalObjectClassInfo(CodeWriter writer, IEnumerable<ClassInfo> collection)
        {
            var graph = new AdjacencyGraph<ClassInfo, IEdge<ClassInfo>>();
            foreach (var item in collection)
            {
                if (EplSystemId.GetType(item.Id) != EplSystemId.Type_Class)
                    continue;
                if (ClassIdMap.TryGetValue(item.BaseClass, out var baseClassInfo))
                {
                    graph.AddVerticesAndEdge(new Edge<ClassInfo>(baseClassInfo, item));
                }
                else
                {
                    graph.AddVertex(item);
                }
            }

            foreach (var item in graph.TopologicalSort())
            {
                DefineInternalObjectClassInfo(writer, item);
            }
        }

        private void DefineInternalObjectClassInfo(CodeWriter writer, ClassInfo classItem)
        {
            var name = GetUserDefinedName_SimpleCppName(classItem.Id);
            var rawName = "raw_" + name;
            writer.NewLine();
            writer.Write($"class {rawName}");
            if (classItem.BaseClass != -1)
            {
                writer.Write(": public ");
                writer.Write(TypeNamespace);
                writer.Write("::eoc_internal::");
                writer.Write("raw_" + GetUserDefinedName_SimpleCppName(classItem.BaseClass));
            }
            else
            {
                writer.Write(": public e::system::basic_object");
            }
            using (writer.NewBlock())
            {
                writer.NewLine();
                if (classItem.Variables.Length != 0)
                {
                    writer.Write("private:");
                    DefineVariable(writer, null, classItem.Variables, false);
                }
                writer.NewLine();
                writer.Write("public:");
                writer.NewLine();
                writer.Write($"{rawName}();");
                writer.NewLine();
                writer.Write($"{rawName}(const {rawName}&);");
                writer.NewLine();
                writer.Write($"virtual ~{rawName}();");
                writer.NewLine();
                writer.Write($"virtual e::system::basic_object* __stdcall clone();");
                foreach (var item in classItem.Method.Select(x => MethodIdMap[x]))
                {
                    DefineMethod(writer, classItem, item);
                }
            }
            writer.Write(";");
        }

        private void ImplementMethod(CodeWriter writer, ClassInfo classItem, MethodInfo item)
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", IdToNameMap.GetUserDefinedName(classItem.Id))
                .Set("method", IdToNameMap.GetUserDefinedName(item.Id)))
            {
                var isClassMember = EplSystemId.GetType(classItem.Id) == EplSystemId.Type_Class;
                var name = GetUserDefinedName_SimpleCppName(item.Id);
                var clsRawName = "raw_" + GetUserDefinedName_SimpleCppName(classItem.Id);
                var eocCmdInfo = GetEocCmdInfo(item);
                WriteMethodHeader(writer, eocCmdInfo, name, false, isClassMember ? clsRawName : null, false);
                using (writer.NewBlock())
                {
                    DefineVariable(writer, null, item.Variables);
                    new CodeConverter(this, classItem, item).Optimize().Generate(writer);
                }
            }
        }

        private void ImplementStaticClass(CodeWriter writer, ClassInfo classItem)
        {
            if (classItem.Variables.Length > 0)
            {
                using (writer.NewNamespace(GetUserDefinedName_SimpleCppName(classItem.Id)))
                {
                    DefineVariable(writer, new string[] { "static" }, classItem.Variables);
                }
            }
            foreach (var item in classItem.Method.Select(x => MethodIdMap[x]))
            {
                ImplementMethod(writer, classItem, item);
            }
        }

        private void ImplementObjectClass(CodeWriter writer, ClassInfo classItem)
        {
            var name = GetUserDefinedName_SimpleCppName(classItem.Id);
            var rawName = "raw_" + name;
            bool hasInitMethod = classItem.Method.Where(x => IdToNameMap.GetUserDefinedName(x) == "_初始化").Count() != 0;
            bool hasDestroyMethod = classItem.Method.Where(x => IdToNameMap.GetUserDefinedName(x) == "_销毁").Count() != 0;
            using (writer.NewNamespace("eoc_internal"))
            {
                writer.NewLine();
                writer.Write($"{rawName}::{rawName}()");
                if (classItem.Variables.Length != 0)
                {
                    writer.Write(": ");
                    InitMembersInConstructor(writer, classItem.Variables);
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
                writer.Write($"{rawName}::~{rawName}()");
                using (writer.NewBlock())
                {
                    if (hasDestroyMethod)
                    {
                        writer.NewLine();
                        writer.Write("this->_销毁();");
                    }
                }

                writer.NewLine();
                writer.Write($"{rawName}::{rawName}(const {rawName}&) = default;");

                writer.NewLine();
                writer.Write($"e::system::basic_object* {rawName}::clone()");
                using (writer.NewBlock())
                {
                    writer.NewLine();
                    writer.Write($"return new {rawName}(*this);");
                }

                foreach (var item in classItem.Method.Select(x => MethodIdMap[x]))
                {
                    ImplementMethod(writer, classItem, item);
                }
            }
        }

        private void InitMembersInConstructor(CodeWriter writer, IEnumerable<AbstractVariableInfo> collection)
        {
            bool first = true;
            foreach (var item in collection)
            {
                if (first)
                    first = false;
                else
                    writer.Write(", ");
                var cppName = GetUserDefinedName_SimpleCppName(item.Id);
                writer.Write(cppName);
                writer.Write("(");
                writer.Write(GetInitParameter(item.DataType, item.UBound));
                writer.Write(")");
            }
        }

        private void DefineVariable(CodeWriter writer, string[] modifiers, AbstractVariableInfo variable, bool initAtOnce = true)
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
            writer.Write(GetCppTypeName(variable.DataType, variable.UBound).ToString());
            writer.Write(" ");
            writer.Write(GetUserDefinedName_SimpleCppName(variable.Id));
            if (initAtOnce)
            {
                var initParameter = GetInitParameter(variable.DataType, variable.UBound);
                if (!string.IsNullOrWhiteSpace(initParameter))
                {
                    writer.Write("(");
                    writer.Write(initParameter);
                    writer.Write(")");
                }
            }
            writer.Write(";");
        }

        private void DefineVariable(CodeWriter writer, string[] modifiers, IEnumerable<AbstractVariableInfo> collection, bool initAtOnce = true)
        {
            foreach (var item in collection)
            {
                DefineVariable(writer, modifiers, item, initAtOnce);
            }
        }

        private void DefineStructNames(CodeWriter writer, IEnumerable<StructInfo> collection)
        {
            using (writer.NewNamespace("eoc_internal"))
            {
                foreach (var item in collection)
                {
                    var name = GetUserDefinedName_SimpleCppName(item.Id);
                    var rawName = "raw_" + name;
                    writer.NewLine();
                    writer.Write($"struct {rawName};");
                }
            }
            foreach (var item in collection)
            {
                var name = GetUserDefinedName_SimpleCppName(item.Id);
                var rawName = "raw_" + name;
                writer.NewLine();
                writer.Write($"typedef e::system::struct_ptr<{TypeNamespace}::eoc_internal::{rawName}> {name};");
            }
        }

        private void DefineInternalStructInfo(CodeWriter writer, StructInfo item)
        {
            var name = GetUserDefinedName_SimpleCppName(item.Id);
            var rawName = "raw_" + name;
            writer.NewLine();
            writer.Write($"struct {rawName}");
            using (writer.NewBlock())
            {
                DefineVariable(writer, null, item.Member, false);

                writer.NewLine();
                writer.Write($"{rawName}()");
                if (item.Member.Length != 0)
                {
                    writer.Write(": ");
                    InitMembersInConstructor(writer, item.Member);
                }
                using (writer.NewBlock())
                {
                }
            }
            writer.Write(";");
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
                result[i] = infos[i].Name ?? $"_AnonymousParameter{i + 1}";
            }
            return result;
        }

        internal void WriteMethodHeader(CodeWriter writer, EocCmdInfo eocCmdInfo, string name, bool isVirtual, string className = null, bool writeDefaultValue = true)
        {
            var paramName = GetParamNameFromInfo(eocCmdInfo.Parameters);
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
                writer.Write(GetParameterTypeString(eocCmdInfo.Parameters[i]));
                writer.Write(" ");
                writer.Write(paramName[i]);
                if (writeDefaultValue && i >= startOfOptionalAtEnd)
                {
                    writer.Write(" = std::nullopt");
                }
            }
            writer.Write(")");
        }

        private void DefineMethod(CodeWriter writer, ClassInfo classItem, MethodInfo item)
        {
            var isClassMember = EplSystemId.GetType(classItem.Id) == EplSystemId.Type_Class;
            var name = GetUserDefinedName_SimpleCppName(item.Id);
            var eocCmdInfo = GetEocCmdInfo(item);
            var isVirtual = false;
            if (isClassMember)
            {
                writer.NewLine();
                writer.Write(item.Public ? "public:" : "private:");
                if (item.Name != "_初始化" && item.Name != "_销毁")
                {
                    isVirtual = true;
                }
            }
            DefineMethod(writer, eocCmdInfo, name, isVirtual);
        }

        private static string GetFileNameByNamespace(string dest, string fullName, string ext)
        {
            return Path.Combine(
                new string[] { dest }.Concat(
                    fullName.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries))
                    .ToArray()) + "." + ext;
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
            return IdToNameMap.GetUserDefinedName(id);
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
            var cppName = GetUserDefinedName_SimpleCppName(id);

            var structInfo = StructIdMap[structId];
            var memberInfo = Array.Find(structInfo.Member, x => x.Id == id);
            var dataType = GetCppTypeName(memberInfo.DataType, memberInfo.UBound);

            return new EocMemberInfo()
            {
                CppName = cppName,
                DataType = dataType
            };
        }

        public EocConstantInfo GetEocConstantInfo(EmnuConstantExpression expr)
        {
            var dataTypeInfo = Libs[expr.LibraryId].DataType[expr.StructId];
            var memberInfo = dataTypeInfo.Member[expr.MemberId];
            var eocConstantInfo = EocLibs[expr.LibraryId].Enum[dataTypeInfo.Name][memberInfo.Name];
            return eocConstantInfo;
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
                    var eocConstantInfo = EocLibs[libraryId].Constant[name];
                    return eocConstantInfo;
            }
        }

        public EocConstantInfo GetEocConstantInfo(int id)
        {
            var cppName = ConstantNamespace + "::" + GetUserDefinedName_SimpleCppName(id);
            string getter = null;
            var constantInfo = ConstantIdMap[id];
            CppTypeName dataType;
            switch (constantInfo.Value)
            {
                case double v:
                    if ((int)v == v)
                    {
                        dataType = CppTypeName_Int;
                    }
                    else if ((long)v == v)
                    {
                        dataType = CppTypeName_Long;
                    }
                    else
                    {
                        dataType = CppTypeName_Double;
                    }
                    break;

                case bool _:
                    dataType = CppTypeName_Bool;
                    break;

                case DateTime _:
                    dataType = CppTypeName_DateTime;
                    break;

                case string _:
                    dataType = CppTypeName_String;
                    getter = cppName;
                    cppName = null;
                    break;

                case byte[] _:
                    dataType = CppTypeName_Bin;
                    getter = cppName;
                    cppName = null;
                    break;

                default:
                    throw new Exception();
            }

            return new EocConstantInfo()
            {
                CppName = cppName,
                Getter = getter,
                DataType = dataType,
                Value = constantInfo.Value
            };
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
                    try
                    {
                        if (Libs[libId] == null)
                        {
                            Logger.Error("缺少fne信息：{0}", this.Source.Code.Libraries[libId].Name);
                            return ErrorEocCmdInfo;
                        }
                        if (Libs[libId].Cmd.Length < id)
                        {
                            Logger.Error("fne信息中缺少命令信息，请检查版本是否匹配【Lib：{0}，CmdId：{1}】", Libs[libId].Name, id);
                            return ErrorEocCmdInfo;
                        }
                        var name = Libs[libId].Cmd[id].Name;
                        if (EocLibs[libId] == null)
                        {
                            Logger.Error("{0} 库缺少Eoc识别信息，可能是EOC不支持该库或没有安装相应Eoc库", Libs[libId].Name);
                            return ErrorEocCmdInfo;
                        }
                        if (!EocLibs[libId].Cmd.ContainsKey(name))
                        {
                            Logger.Error("{0} 命令缺少Eoc识别信息，可能是EOC不支持该命令或没有安装相应Eoc库", name);
                            return ErrorEocCmdInfo;
                        }
                        return EocLibs[libId].Cmd[name];
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("获取Eoc命令信息时出现未知错误【Lib：{0}，CmdId：{1}】：{2}", Libs[libId].Name, id, ex);
                        return ErrorEocCmdInfo;
                    }
            }
        }

        public EocCmdInfo GetEocCmdInfo(int id)
        {
            switch (EplSystemId.GetType(id))
            {
                case EplSystemId.Type_Method:
                    return GetEocCmdInfo(MethodIdMap[id]);

                case EplSystemId.Type_Dll:
                    return GetEocCmdInfo(DllIdMap[id]);

                default:
                    throw new Exception();
            }
        }

        public EocCmdInfo GetEocCmdInfo(DllDeclareInfo x)
        {
            return new EocCmdInfo()
            {
                ReturnDataType = x.ReturnDataType == 0 ? null : GetCppTypeName(x.ReturnDataType),
                CppName = GetCppMethodName(x.Id),
                Parameters = x.Parameters.Select(GetEocParameterInfo).ToList()
            };
        }

        public EocCmdInfo GetEocCmdInfo(MethodInfo x)
        {
            return new EocCmdInfo()
            {
                ReturnDataType = x.ReturnDataType == 0 ? null : GetCppTypeName(x.ReturnDataType),
                CppName = GetCppMethodName(x.Id),
                Parameters = x.Parameters.Select(GetEocParameterInfo).ToList()
            };
        }

        public EocParameterInfo GetEocParameterInfo(MethodParameterInfo x)
        {
            return new EocParameterInfo()
            {
                ByRef = x.ByRef || x.ArrayParameter || !IsValueType(x.DataType),
                Optional = x.OptionalParameter,
                VarArgs = false,
                DataType = GetCppTypeName(x.DataType, x.ArrayParameter),
                Name = GetUserDefinedName_SimpleCppName(x.Id)
            };
        }

        public EocParameterInfo GetEocParameterInfo(DllParameterInfo x)
        {
            return new EocParameterInfo()
            {
                ByRef = x.ByRef || x.ArrayParameter || !IsValueType(x.DataType),
                Optional = false,
                VarArgs = false,
                DataType = GetCppTypeName(x.DataType, x.ArrayParameter),
                Name = GetUserDefinedName_SimpleCppName(x.Id)
            };
        }

        public string GetParameterTypeString(EocParameterInfo x)
        {
            var r = x.DataType.ToString();
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

        #region TypeInfoHelper

        public CppTypeName GetCppTypeName(int id, int[] uBound)
        {
            return GetCppTypeName(id, uBound != null && uBound.Length != 0);
        }

        public CppTypeName GetCppTypeName(int id, bool isArray = false)
        {
            id = TranslateDataTypeId(id);
            if (id == DataTypeId_IntPtr)
            {
                return CppTypeName_IntPtr;
            }
            if (!BasicCppTypeNameMap.TryGetValue(id, out var result))
            {
                if (EplSystemId.GetType(id) == EplSystemId.Type_Class
                    || EplSystemId.GetType(id) == EplSystemId.Type_Struct)
                {
                    result = new CppTypeName(false, TypeNamespace + "::" + GetUserDefinedName_SimpleCppName(id));
                }
                else
                {
                    EplSystemId.DecomposeLibDataTypeId(id, out var libId, out var typeId);
                    var name = Libs[libId].DataType[typeId].Name;
                    result = EocLibs[libId].Type[name].CppName;
                }
            }
            if (isArray)
                result = new CppTypeName(false, "e::system::array", new[] { result });
            return result;
        }

        public int TranslateDataTypeId(int dataType)
        {
            if (dataType == 0)
                return EplSystemId.DataType_Int;
            if (EplSystemId.IsLibDataType(dataType))
            {
                EplSystemId.DecomposeLibDataTypeId(dataType, out var libId, out var typeId);
                try
                {
                    if (EocLibs[libId].Enum.ContainsKey(Libs[libId].DataType[typeId].Name))
                        return EplSystemId.DataType_Int;
                }
                catch (Exception)
                {
                }
            }
            return dataType;
        }

        /// <summary>
        /// 不保证与字节数相等，只保证数值大的类型大
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public int GetIntNumberTypeSize(CppTypeName dataType)
        {
            if (dataType == CppTypeName_Byte)
            {
                return 1;
            }
            else if (dataType == CppTypeName_Short)
            {
                return 2;
            }
            else if (dataType == CppTypeName_Int)
            {
                return 4;
            }
            else if (dataType == CppTypeName_Long)
            {
                return 8;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// 不保证与字节数相等，只保证数值大的类型大
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public int GetIntNumberTypeSize(int dataType)
        {
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Byte:
                    return 1;

                case EplSystemId.DataType_Short:
                    return 2;

                case EplSystemId.DataType_Int:
                    return 4;

                case EplSystemId.DataType_Long:
                    return 8;

                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// 不保证与字节数相等，只保证数值大的类型大
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public int GetFloatNumberTypeSize(CppTypeName dataType)
        {
            if (dataType == CppTypeName_Float)
            {
                return 4;
            }
            else if (dataType == CppTypeName_Double)
            {
                return 8;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// 不保证与字节数相等，只保证数值大的类型大
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public int GetFloatNumberTypeSize(int dataType)
        {
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Float:
                    return 4;

                case EplSystemId.DataType_Double:
                    return 8;

                default:
                    throw new ArgumentException();
            }
        }

        public bool IsFloatNumberType(CppTypeName dataType)
        {
            if (dataType == CppTypeName_Float
                || dataType == CppTypeName_Double)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsFloatNumberType(int dataType)
        {
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Float:
                case EplSystemId.DataType_Double:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsIntNumberType(CppTypeName dataType)
        {
            if (dataType == CppTypeName_Byte
                || dataType == CppTypeName_Short
                || dataType == CppTypeName_Int
                || dataType == CppTypeName_Long)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsIntNumberType(int dataType)
        {
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Byte:
                case EplSystemId.DataType_Int:
                case EplSystemId.DataType_Long:
                case EplSystemId.DataType_Short:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsValueType(CppTypeName dataType)
        {
            if (dataType == CppTypeName_Bool
                || dataType == CppTypeName_Byte
                || dataType == CppTypeName_Short
                || dataType == CppTypeName_Int
                || dataType == CppTypeName_Long
                || dataType == CppTypeName_Float
                || dataType == CppTypeName_Double
                || dataType == CppTypeName_DateTime
                || dataType == CppTypeName_MethodPtr
                || dataType == CppTypeName_IntPtr)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsValueType(int dataType)
        {
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Bool:
                case EplSystemId.DataType_Byte:
                case EplSystemId.DataType_DateTime:
                case EplSystemId.DataType_Double:
                case EplSystemId.DataType_Float:
                case EplSystemId.DataType_Int:
                case EplSystemId.DataType_Long:
                case EplSystemId.DataType_Short:
                case EplSystemId.DataType_MethodPtr:
                case var x when x == DataTypeId_IntPtr:
                    return true;

                default:
                    return false;
            }
        }

        public string GetInitValue(int dataType, bool isArray)
        {
            return GetInitValue(dataType, isArray ? new int[] { 0 } : null);
        }

        public string GetInitValue(int dataType, int[] uBound)
        {
            return GetCppTypeName(dataType, uBound != null && uBound.Length != 0) + "(" + GetInitParameter(dataType, uBound) + ")";
        }

        public string GetInitParameter(int dataType, bool isArray)
        {
            return GetInitParameter(dataType, isArray ? new int[] { 0 } : null);
        }

        public string GetInitParameter(int dataType, int[] uBound)
        {
            if (uBound != null && uBound.Length != 0)
            {
                if (uBound[0] == 0)
                {
                    return "nullptr";
                }
                return string.Join(", ", uBound.Select(x => x + "u"));
            }
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Bool:
                    return "false";

                case EplSystemId.DataType_Byte:
                case EplSystemId.DataType_DateTime:
                case EplSystemId.DataType_Double:
                case EplSystemId.DataType_Float:
                case EplSystemId.DataType_Int:
                case EplSystemId.DataType_Long:
                case EplSystemId.DataType_Short:
                case var x when x == DataTypeId_IntPtr:
                    return "0";

                case EplSystemId.DataType_MethodPtr:
                    return "nullptr";

                default:
                    return "";
            }
        }

        public string GetNullParameter(int dataType, bool isArray = false)
        {
            if (isArray)
            {
                return "nullptr";
            }
            dataType = TranslateDataTypeId(dataType);
            switch (dataType)
            {
                case EplSystemId.DataType_Bool:
                    return "false";

                case EplSystemId.DataType_Byte:
                case EplSystemId.DataType_DateTime:
                case EplSystemId.DataType_Double:
                case EplSystemId.DataType_Float:
                case EplSystemId.DataType_Int:
                case EplSystemId.DataType_Long:
                case EplSystemId.DataType_Short:
                case var x when x == DataTypeId_IntPtr:
                    return "0";

                default:
                    return "nullptr";
            }
        }

        #endregion TypeInfoHelper
    }
}