using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public enum CppPtrType
    {
        Normal,
        Const
    }

    [JsonConverter(typeof(CppTypeNameConverter))]
    public class CppTypeName : IEquatable<CppTypeName>
    {
        private class SimpleLexer
        {
            private int i = 0;
            private readonly string str;
            private static char[] Spaces = new[] { ' ', '\t' };
            private static char[] Symbols = new[] { ',', '<', '>', '*' };
            private static char[] SpacesAndSymbols = Spaces.Concat(Symbols).ToArray();

            public SimpleLexer(string str)
            {
                this.str = str;
            }

            public string PeekToken()
            {
                var i_backup = i;
                var result = NextToken();
                i = i_backup;
                return result;
            }

            public string NextToken()
            {
                if (i >= str.Length)
                    return null;
                while (Array.IndexOf(Spaces, str[i]) != -1)
                {
                    i++;
                    if (i >= str.Length)
                        return null;
                }
                var start = i;
                i = str.IndexOfAny(SpacesAndSymbols, i);
                if (start == i) //当前为符号
                {
                    i = i + 1;
                    return str.Substring(start, 1);
                }
                if (i == -1)
                {
                    i = str.Length;
                }
                return str.Substring(start, i - start);
            }
        }

        public bool IsConst { get; }
        public string Name { get; }
        public ReadOnlyCollection<CppTypeName> TypeParam { get; }
        public ReadOnlyCollection<CppPtrType> PtrInfos { get; }

        public CppTypeName(bool isConst, string name, IList<CppTypeName> typeParam, IList<CppPtrType> ptrInfos = null)
            : this(isConst,
                  name,
                  typeParam == null ? null : new ReadOnlyCollection<CppTypeName>(typeParam),
                  ptrInfos == null ? null : new ReadOnlyCollection<CppPtrType>(ptrInfos))
        {
        }

        public CppTypeName(bool isConst, string name, ReadOnlyCollection<CppTypeName> typeParam = null, ReadOnlyCollection<CppPtrType> ptrInfos = null)
        {
            IsConst = isConst;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TypeParam = typeParam;
            PtrInfos = ptrInfos;
        }

        private static CppTypeName Parse(SimpleLexer lexer)
        {
            var ptrInfos = new List<CppPtrType>();

            var isConst = false;
            var name = lexer.NextToken();
            while (name == "const")
            {
                isConst = true;
                name = lexer.NextToken();
            }
            string nextToken;
            while ((nextToken = lexer.PeekToken()) == "const")
            {
                lexer.NextToken(); // "const"
                isConst = true;
                nextToken = lexer.PeekToken();
            }

            List<CppTypeName> typeParam = null;
            if (nextToken == "<")
            {
                typeParam = new List<CppTypeName>();
                lexer.NextToken(); // "<"
                if (lexer.PeekToken() != ">")
                {
                    typeParam.Add(Parse(lexer));
                    while ((nextToken = lexer.NextToken()) == ",")
                    {
                        typeParam.Add(Parse(lexer));
                    }
                    if (nextToken != ">")
                    {
                        throw new Exception("解析Cpp类型名错误，'<' 与 '>' 不匹配");
                    }
                }
                else
                {
                    lexer.NextToken(); // ">"
                }
            }
            while ((nextToken = lexer.PeekToken()) == "const")
            {
                lexer.NextToken(); // "const"
                isConst = true;
                nextToken = lexer.PeekToken();
            }
            while (nextToken == "*")
            {
                lexer.NextToken(); // "*"

                var ptrInfo = CppPtrType.Normal;
                while ((nextToken = lexer.PeekToken()) == "const")
                {
                    ptrInfo = CppPtrType.Const;
                    lexer.NextToken(); //const
                    nextToken = lexer.PeekToken();
                }

                ptrInfos.Add(ptrInfo);
            }
            if (ptrInfos.Count == 0)
            {
                ptrInfos = null;
            }
            return new CppTypeName(isConst, name, typeParam, ptrInfos);
        }

        public static CppTypeName Parse(string str)
        {
            if (str.Trim() == "*")
            {
                return new CppTypeName(false, "*");
            }

            var lexer = new SimpleLexer(str);
            var result = Parse(lexer);
            if (lexer.NextToken() != null)
            {
                throw new Exception("解析Cpp类型名错误，出现意外的尾随数据");
            }
            return result;
        }

        private void ToString(StringBuilder builder)
        {
            if (IsConst)
                builder.Append("const ");
            builder.Append(Name);
            if (TypeParam != null)
            {
                builder.Append("<");
                for (int i = 0; i < TypeParam.Count; i++)
                {
                    if (i != 0)
                        builder.Append(", ");
                    TypeParam[i].ToString(builder);
                }
                if (builder[builder.Length - 1] == '>')
                    builder.Append(" ");
                builder.Append(">");
            }
            if (PtrInfos != null)
            {
                foreach (var item in PtrInfos)
                {
                    switch (item)
                    {
                        case CppPtrType.Normal:
                            builder.Append(" *");
                            break;

                        case CppPtrType.Const:
                            builder.Append(" *const");
                            break;

                        default:
                            throw new Exception();
                    }
                }
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            ToString(builder);
            return builder.ToString();
        }

        #region Equals/GetHashCode

        public override bool Equals(object obj)
        {
            return Equals(obj as CppTypeName);
        }

        public bool Equals(CppTypeName other)
        {
            if (other == null)
                return false;

            if (IsConst != other.IsConst)
                return false;

            if (Name != other.Name)
                return false;

            if (TypeParam == null || TypeParam.Count == 0)
            {
                if (other.TypeParam != null && other.TypeParam.Count != 0)
                    return false;
            }
            else
            {
                if (!TypeParam.SequenceEqual(other.TypeParam))
                    return false;
            }

            if (PtrInfos == null || PtrInfos.Count == 0)
            {
                if (other.PtrInfos != null && other.PtrInfos.Count != 0)
                    return false;
            }
            else
            {
                if (!PtrInfos.SequenceEqual(other.PtrInfos))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = -2115335014;
            hashCode = hashCode * -1521134295 + IsConst.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            if (TypeParam != null)
            {
                foreach (var item in TypeParam)
                {
                    hashCode = hashCode * -1521134295 + EqualityComparer<CppTypeName>.Default.GetHashCode(item);
                }
            }
            if (PtrInfos != null)
            {
                foreach (var item in PtrInfos)
                {
                    hashCode = hashCode * -1521134295 + EqualityComparer<CppPtrType>.Default.GetHashCode(item);
                }
            }
            return hashCode;
        }

        public static bool operator ==(CppTypeName name1, CppTypeName name2)
        {
            return EqualityComparer<CppTypeName>.Default.Equals(name1, name2);
        }

        public static bool operator !=(CppTypeName name1, CppTypeName name2)
        {
            return !(name1 == name2);
        }

        #endregion Equals/GetHashCode
    }
}