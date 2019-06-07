using System;
using System.IO;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public class CodeWriter : IDisposable
    {
        private StreamWriter streamWriter;

        public int Indent { get; set; }

        public CodeWriter(StreamWriter streamWriter)
        {
            this.streamWriter = streamWriter ?? throw new ArgumentNullException(nameof(streamWriter));
        }

        public CodeWriter(string fileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            this.streamWriter = new StreamWriter(File.Create(fileName), Encoding.Default);
        }

        public void Write(string value)
        {
            streamWriter.Write(value);
        }

        public void WriteLiteral(object value)
        {
            switch (value)
            {
                case byte v:
                    WriteLiteral(v);
                    break;
                case short v:
                    WriteLiteral(v);
                    break;
                case int v:
                    WriteLiteral(v);
                    break;
                case long v:
                    WriteLiteral(v);
                    break;
                case IntPtr v:
                    WriteLiteral(v);
                    break;
                case float v:
                    WriteLiteral(v);
                    break;
                case double v:
                    WriteLiteral(v);
                    break;
                case DateTime v:
                    WriteLiteral(v);
                    break;
                case bool v:
                    WriteLiteral(v);
                    break;
                case string v:
                    WriteLiteral(v);
                    break;
                default:
                    throw new ArgumentException(nameof(value));
            }
        }
        public void WriteLiteral(DateTime value)
        {
            streamWriter.Write("e::system::datetime(");
            streamWriter.Write(value.ToOADate().ToString("G17"));
            streamWriter.Write("/*");
            streamWriter.Write(value.ToString("yyyyMMddTHHmmss"));
            streamWriter.Write("*/");
            streamWriter.Write(")");
        }

        public void WriteLiteral(byte value)
        {
            streamWriter.Write("UINT8_C(");
            streamWriter.Write(value);
            streamWriter.Write(")");
        }

        public void WriteLiteral(short value)
        {
            streamWriter.Write("INT16_C(");
            streamWriter.Write(value);
            streamWriter.Write(")");
        }

        public void WriteLiteral(int value)
        {
            streamWriter.Write(value);
        }

        public void WriteLiteral(long value)
        {
            streamWriter.Write("INT64_C(");
            streamWriter.Write(value);
            streamWriter.Write(")");
        }
        public void WriteLiteral(IntPtr value)
        {
            streamWriter.Write("intptr_t(");
            try
            {
                streamWriter.Write(value.ToInt32());
            }
            catch (OverflowException)
            {
                streamWriter.Write(value.ToInt64());
                streamWriter.Write("ll");
            }
            streamWriter.Write(")");
        }
        public void WriteLiteral(float value)
        {
            streamWriter.Write(value.ToString("G9"));
            streamWriter.Write("f");
        }
        public void WriteLiteral(double value)
        {
            long lv = (long)value;
            if (lv == value)
            {
                streamWriter.Write(lv);
                streamWriter.Write(".0");
            }
            else
            {
                streamWriter.Write("double(");
                streamWriter.Write(value.ToString("G17"));
                streamWriter.Write(")");
            }
        }
        public void WriteLiteral(bool value)
        {
            streamWriter.Write(value ? "true" : "false");
        }

        public void WriteLiteral(string value)
        {
            streamWriter.Write("EOC_STR_CONST(");
            WriteCppStringLiteral(value);
            streamWriter.Write(")");
        }

        public void WriteCppStringLiteral(string value)
        {
            streamWriter.Write('"');
            streamWriter.Write(value.Replace("\\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t"));
            streamWriter.Write('"');
        }

        public void AddComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                Write("// ");
                Write(comment);
            }
        }

        public void AddCommentLine(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                NewLine();
                Write("// ");
                Write(comment);
            }
        }

        public void NewLine()
        {
            streamWriter.WriteLine();
            for (int i = 0; i < Indent; i++)
            {
                streamWriter.Write("    ");
            }
        }

        public struct WriteBlockHelper : IDisposable
        {
            private CodeWriter writer;

            internal WriteBlockHelper(CodeWriter writer)
            {
                this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
                writer.NewLine();
                writer.Write("{");
                writer.Indent++;
            }

            public void Dispose()
            {
                writer.Indent--;
                writer.NewLine();
                writer.Write("}");
            }
        }

        public WriteBlockHelper NewBlock()
        {
            return new WriteBlockHelper(this);
        }

        public struct WriteNamespaceHelper : IDisposable
        {
            private CodeWriter writer;
            private string[] splitedNamespace;

            internal WriteNamespaceHelper(CodeWriter writer, string rawNamespace) : this(writer, rawNamespace.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries))
            {
            }

            internal WriteNamespaceHelper(CodeWriter writer, params string[] splitedNamespace)
            {
                this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
                this.splitedNamespace = splitedNamespace ?? throw new ArgumentNullException(nameof(splitedNamespace));
                foreach (var item in splitedNamespace)
                {
                    writer.NewLine();
                    writer.Write("namespace " + item);
                    writer.NewLine();
                    writer.Write("{");
                    writer.Indent++;
                }
            }

            public void Dispose()
            {
                foreach (var item in splitedNamespace)
                {
                    writer.Indent--;
                    writer.NewLine();
                    writer.Write("}");
                }
            }
        }

        public WriteNamespaceHelper NewNamespace(string projectNamespace)
        {
            return new WriteNamespaceHelper(this, projectNamespace);
        }

        public void Dispose()
        {
            streamWriter.Dispose();
        }
    }
}