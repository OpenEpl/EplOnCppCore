using System;
using System.Collections.Generic;
using System.Text;

namespace QIQI.EplOnCpp.Core
{
    public interface ILoggerWithContext
    {
        ILoggerWithContext SetContextInfo(object key, object value);
        ILoggerWithContext PushContextInfo();
        ILoggerWithContext PopContextInfo();
        ILoggerWithContext Info(string format, params object[] args);
        ILoggerWithContext Warn(string format, params object[] args);
        ILoggerWithContext Error(string format, params object[] args);
        ILoggerWithContext Debug(string format, params object[] args);
    }

    public class NullLoggerWithContext : ILoggerWithContext
    {
        public ILoggerWithContext SetContextInfo(object key, object value) => this;
        public ILoggerWithContext PushContextInfo() => this;
        public ILoggerWithContext PopContextInfo() => this;
        public ILoggerWithContext Debug(string format, params object[] args) => this;
        public ILoggerWithContext Error(string format, params object[] args) => this;
        public ILoggerWithContext Info(string format, params object[] args) => this;
        public ILoggerWithContext Warn(string format, params object[] args) => this;
    }

    public struct LoggerContextHelper : IDisposable
    {
        private readonly ILoggerWithContext logger;

        public LoggerContextHelper(ILoggerWithContext logger)
        {
            logger.PushContextInfo();
            this.logger = logger;
        }
        public LoggerContextHelper Set(object key, object value)
        {
            logger.SetContextInfo(key, value);
            return this;
        }
        public void Dispose()
        {
            logger.PopContextInfo();
        }
    }
}
