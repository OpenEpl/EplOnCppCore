using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public static class EocDataTypes
    {
        public static CppTypeName Bin { get; } = new CppTypeName(false, "e::system::bin");
        public static CppTypeName Bool { get; } = new CppTypeName(false, "bool");
        public static CppTypeName Byte { get; } = new CppTypeName(false, "uint8_t");
        public static CppTypeName DateTime { get; } = new CppTypeName(false, "e::system::datetime");
        public static CppTypeName Double { get; } = new CppTypeName(false, "double");
        public static CppTypeName Float { get; } = new CppTypeName(false, "float");
        public static CppTypeName Int { get; } = new CppTypeName(false, "int32_t");
        public static CppTypeName Long { get; } = new CppTypeName(false, "int64_t");
        public static CppTypeName Short { get; } = new CppTypeName(false, "int16_t");
        public static CppTypeName IntPtr { get; } = new CppTypeName(false, "intptr_t");
        public static CppTypeName MethodPtr { get; } = new CppTypeName(false, "e::system::methodptr");
        public static CppTypeName String { get; } = new CppTypeName(false, "e::system::string");
        public static CppTypeName Any { get; } = new CppTypeName(false, "e::system::any");
        public static CppTypeName Auto { get; } = new CppTypeName(false, "*");
        public static CppTypeName ErrorType { get; } = new CppTypeName(false, "EOC_ERROR_TYPE");

        public static Dictionary<int, CppTypeName> BasicTypeMap { get; } = new Dictionary<int, CppTypeName> {
            { EplSystemId.DataType_Bin , Bin },
            { EplSystemId.DataType_Bool , Bool },
            { EplSystemId.DataType_Byte , Byte },
            { EplSystemId.DataType_DateTime , DateTime },
            { EplSystemId.DataType_Double , Double },
            { EplSystemId.DataType_Float , Float },
            { EplSystemId.DataType_Int , Int },
            { EplSystemId.DataType_Long , Long },
            { EplSystemId.DataType_Short , Short },
            { EplSystemId.DataType_MethodPtr , MethodPtr },
            { EplSystemId.DataType_String , String },
            { EplSystemId.DataType_Any , Any }
        };

        private static Dictionary<Type, CppTypeName> ConstTypeMap { get; } = new Dictionary<Type, CppTypeName>()
        {
            { typeof(byte), Byte },
            { typeof(short), Short },
            { typeof(int), Int },
            { typeof(long), Long },
            { typeof(float), Float },
            { typeof(double), Double },
            { typeof(IntPtr), IntPtr },
            { typeof(DateTime), DateTime },
            { typeof(string), String },
            { typeof(bool), Bool }
        };

        public static CppTypeName ArrayOf(CppTypeName elemType)
        {
            return new CppTypeName(false, "e::system::array", new[] { elemType });
        }

        public static CppTypeName GetConstValueType(object value)
        {
            var type = value.GetType();
            var isArray = type.IsArray;
            while (type.IsArray)
            {
                type = type.GetElementType();
            }

            return isArray
                ? ArrayOf(ConstTypeMap[type])
                : ConstTypeMap[type];
        }


        public static int NormalizeDataTypeId(ProjectConverter P, int dataType)
        {
            if (dataType == 0)
                return EplSystemId.DataType_Int;
            if (EplSystemId.IsLibDataType(dataType) && dataType != P.DataTypeId_IntPtr)
            {
                EplSystemId.DecomposeLibDataTypeId(dataType, out var libId, out var typeId);
                try
                {
                    if (P.Libs[libId].DataType[typeId].IsEnum)
                        return EplSystemId.DataType_Int;
                }
                catch (Exception)
                {
                }
                try
                {
                    if (P.EocLibs[libId].Enum.ContainsKey(P.Libs[libId].DataType[typeId].Name))
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
        public static int GetIntNumberTypeSize(CppTypeName dataType)
        {
            if (dataType == Byte)
            {
                return 1;
            }
            else if (dataType == Short)
            {
                return 2;
            }
            else if (dataType == Int)
            {
                return 4;
            }
            else if (dataType == Long)
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
        public static int GetFloatNumberTypeSize(CppTypeName dataType)
        {
            if (dataType == Float)
            {
                return 4;
            }
            else if (dataType == Double)
            {
                return 8;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static bool IsFloatNumberType(CppTypeName dataType)
        {
            if (dataType == Float
                || dataType == Double)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsIntNumberType(CppTypeName dataType)
        {
            if (dataType == Byte
                || dataType == Short
                || dataType == Int
                || dataType == Long)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsArithmeticType(CppTypeName dataType)
        {
            return IsIntNumberType(dataType) || IsFloatNumberType(dataType) || dataType == IntPtr;
        }

        public static bool IsValueType(CppTypeName dataType)
        {
            if (dataType == Bool
                || dataType == DateTime
                || dataType == MethodPtr
                || IsArithmeticType(dataType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static CppTypeName Translate(ProjectConverter P, int id, int[] uBound)
        {
            return Translate(P, id, uBound != null && uBound.Length != 0);
        }

        public static CppTypeName Translate(ProjectConverter P, int id, bool isArray = false)
        {
            id = NormalizeDataTypeId(P, id);
            if (id == P.DataTypeId_IntPtr)
            {
                return IntPtr;
            }
            if (!BasicTypeMap.TryGetValue(id, out var result))
            {
                if (EplSystemId.GetType(id) == EplSystemId.Type_Class
                    || EplSystemId.GetType(id) == EplSystemId.Type_Struct)
                {
                    result = new CppTypeName(false, P.TypeNamespace + "::" + P.GetUserDefinedName_SimpleCppName(id));
                }
                else
                {
                    EplSystemId.DecomposeLibDataTypeId(id, out var libId, out var typeId);

                    if (P.Libs[libId] == null)
                    {
                        return ErrorType;
                    }
                    if (typeId >= P.Libs[libId].DataType.Length)
                    {
                        return ErrorType;
                    }
                    var name = P.Libs[libId].DataType[typeId].Name;
                    if (P.EocLibs[libId] == null)
                    {
                        return ErrorType;
                    }
                    if (!P.EocLibs[libId].Type.ContainsKey(name))
                    {
                        return ErrorType;
                    }
                    result = P.EocLibs[libId].Type[name].CppName;
                }
            }
            if (isArray)
                result = new CppTypeName(false, "e::system::array", new[] { result });
            return result;
        }

        public static string GetInitParameter(CppTypeName dataType, List<int> uBound = null)
        {
            if (dataType.Name == "e::system::array")
            {
                if (uBound != null && uBound.Count != 0 && uBound[0] != 0)
                {
                    return string.Join(", ", uBound.Select(x => x + "u"));
                }
            }
            if (dataType == Bool)
            {
                return "false";
            }
            if (IsArithmeticType(dataType))
            {
                return "0";
            }
            if (dataType == DateTime)
            {
                return "0";
            }
            if (dataType == MethodPtr)
            {
                return "nullptr";
            }
            return $"e::system::default_value<{dataType}>::value()";
        }
    }
}
