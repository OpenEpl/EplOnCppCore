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

        public void WriteStringLiteral(string value)
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