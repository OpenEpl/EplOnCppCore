using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class EocConstant
    {
        public ProjectConverter P;

        public EocConstant(ProjectConverter p, string name, EocConstantInfo info)
        {
            P = p ?? throw new ArgumentNullException(nameof(p));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public string Name { get; }
        public EocConstantInfo Info { get; }

        private void DefineItem(CodeWriter writer)
        {
            if (Info.Value == null)
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
                    throw new Exception();
                default:
                    throw new Exception();
            }
        }

        public static EocConstant[] Translate(ProjectConverter P, IEnumerable<ConstantInfo> constantInfos)
        {
            return constantInfos.Select(x => Translate(P, x)).ToArray();
        }

        public static EocConstant Translate(ProjectConverter P, ConstantInfo constantInfo)
        {
            return new EocConstant(P, P.GetUserDefinedName_SimpleCppName(constantInfo.Id), P.GetEocConstantInfo(constantInfo.Id));
        }

        public static void Define(ProjectConverter P, CodeWriter writer, EocConstant[] eocDlls)
        {
            writer.Write("#pragma once");
            writer.NewLine();
            writer.Write("#include <e/system/basic_type.h>");
            using (writer.NewNamespace(P.ConstantNamespace))
            {
                foreach (var item in eocDlls)
                {
                    item.DefineItem(writer);
                }
            }
        }
    }
}
