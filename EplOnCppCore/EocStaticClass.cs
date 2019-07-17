using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class EocStaticClass
    {
        public ProjectConverter P { get; }
        public ClassInfo RawInfo { get; }
        public string Name { get; }
        public string CppName { get; }
        public List<CodeConverter> Method { get; }

        public EocStaticClass(ProjectConverter p, ClassInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
            Name = P.GetUserDefinedName_SimpleCppName(RawInfo.Id);
            CppName = $"{P.CmdNamespace}::{Name}";
            Method = RawInfo.Method.Select(x => P.MethodIdMap[x]).Select(x => new CodeConverter(P, RawInfo, x)).ToList();
        }
        public void Define(CodeWriter writer)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"../type.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                foreach (var item in Method)
                {
                    item.DefineItem(writer);
                }
            }
        }


        public void Implement(CodeWriter writer)
        {
            writer.Write("#include \"../../../stdafx.h\"");
            using (writer.NewNamespace(P.CmdNamespace))
            {
                if (RawInfo.Variables.Length > 0)
                {
                    using (writer.NewNamespace(Name))
                    {
                        P.DefineVariable(writer, new string[] { "static" }, RawInfo.Variables);
                    }
                }
                foreach (var item in Method)
                {
                    item.ImplementItem(writer);
                }
            }
        }

        public static EocStaticClass[] Translate(ProjectConverter P, IEnumerable<ClassInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocStaticClass Translate(ProjectConverter P, ClassInfo rawInfo)
        {
            return new EocStaticClass(P, rawInfo);
        }
    }
}