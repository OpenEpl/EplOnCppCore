using System.Collections.Generic;
using System.IO;

namespace QIQI.EplOnCpp.Core
{
    public class StreamLoggerWithContext : ILoggerWithContext
    {
        private readonly TextWriter DebugStream;
        private readonly TextWriter InfoStream;
        private readonly TextWriter WarnStream;
        private readonly TextWriter ErrorStream;

        private Stack<Dictionary<object, object>> Context = new Stack<Dictionary<object, object>>();

        public StreamLoggerWithContext(TextWriter stdOut, TextWriter stdErr, bool debug = false) : this(debug ? stdOut : null, stdOut, stdOut, stdErr)
        {
        }

        public StreamLoggerWithContext(TextWriter debugStream, TextWriter infoStream, TextWriter warnStream, TextWriter errorStream)
        {
            Context.Push(new Dictionary<object, object>());
            DebugStream = debugStream;
            InfoStream = infoStream;
            WarnStream = warnStream;
            ErrorStream = errorStream;
        }

        public ILoggerWithContext Debug(string format, params object[] args) => WriteInfo("Debug", DebugStream, format, args);

        public ILoggerWithContext Info(string format, params object[] args) => WriteInfo("Info", InfoStream, format, args);

        public ILoggerWithContext Warn(string format, params object[] args) => WriteInfo("Warn", WarnStream, format, args);

        public ILoggerWithContext Error(string format, params object[] args) => WriteInfo("Error", ErrorStream, format, args);

        private StreamLoggerWithContext WriteInfo(string type, TextWriter writer, string format, params object[] args)
        {
            if (writer == null)
                return this;
            writer.Write("[");
            writer.Write(type);
            writer.Write("] ");
            writer.WriteLine(format, args);
            WriteContextInfo(writer);
            return this;
        }

        private void WriteContextInfo(TextWriter writer)
        {
            if (Context.Peek().Count == 0)
                return;
            writer.WriteLine("\tContext:");
            foreach (var x in Context.Peek())
            {
                writer.Write("\t\t");
                writer.Write(x.Key);
                writer.Write(": ");
                writer.Write(x.Value);
                writer.WriteLine();
            }
        }

        public ILoggerWithContext PopContextInfo()
        {
            Context.Pop();
            return this;
        }

        public ILoggerWithContext PushContextInfo()
        {
            Context.Push(new Dictionary<object, object>(Context.Peek()));
            return this;
        }

        public ILoggerWithContext SetContextInfo(object key, object value)
        {
            Context.Peek()[key] = value;
            return this;
        }
    }
}