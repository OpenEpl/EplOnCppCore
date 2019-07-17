using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocGlobalVariable
    {
        public ProjectConverter P { get; }

        public GlobalVariableInfo RawInfo { get; }

        public EocGlobalVariable(ProjectConverter p, GlobalVariableInfo rawInfo)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            RawInfo = rawInfo ?? throw new ArgumentNullException(nameof(rawInfo));
        }

        private void DefineItem(CodeWriter writer)
        {
            P.DefineVariable(writer, new string[] { "extern" }, RawInfo, false);
        }

        private void ImplementItem(CodeWriter writer)
        {
            P.DefineVariable(writer, null, RawInfo);
        }

        public static EocGlobalVariable[] Translate(ProjectConverter P, IEnumerable<GlobalVariableInfo> rawInfo)
        {
            return rawInfo.Select(x => Translate(P, x)).ToArray();
        }

        public static EocGlobalVariable Translate(ProjectConverter P, GlobalVariableInfo rawInfo)
        {
            return new EocGlobalVariable(P, rawInfo);
        }

        public static void Define(ProjectConverter P, CodeWriter writer, EocGlobalVariable[] collection)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"type.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in collection)
                {
                    item.DefineItem(writer);
                }
            }
        }

        public static void Implement(ProjectConverter P, CodeWriter writer, EocGlobalVariable[] collection)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include \"global.h\"");
            using (writer.NewNamespace(P.GlobalNamespace))
            {
                foreach (var item in collection)
                {
                    item.ImplementItem(writer);
                }
            }
        }
    }
}
