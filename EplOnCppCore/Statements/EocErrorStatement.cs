using System;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocErrorStatement : EocStatement
    {
        public EocErrorStatement(CodeConverter c, Exception exception, string line) : base(c)
        {
            Exception = exception;
            Line = line;
        }

        public Exception Exception { get; }
        public string Line { get; }

        public override void WriteTo(CodeWriter writer)
        {
            using (new LoggerContextHelper(Logger)
                .Set("line", Line))
            {
                Logger.Error("分析语句时出错，错误信息：{0}", Exception);
            }
            writer.NewLine();
            writer.Write("EOC_ERROR_STATEMENT;");
        }
    }
}