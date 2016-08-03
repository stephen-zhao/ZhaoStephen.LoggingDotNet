using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhaoStephen.LoggingDotNet
{
    public class LogMsg
    {
        public string Message { get; protected set; } = "";
        public LogSeverityLvls Severity { get; protected set; } = LogSeverityLvls.DEBUG;
        public bool IsThreadTerminatingMsg { get; protected set; } = false;
        public DateTime TimeStamp { get; protected set; } = DateTime.MinValue;
        public int CallerLineNumber { get; protected set; } = 0;
        public string CallerFilePath { get; protected set; } = "";
        public string CallerMemberName { get; protected set; } = "";

        public static LogMsg GetThreadTerminatingMsg()
        {
            LogMsg msgTerm = new LogMsg();
            msgTerm.IsThreadTerminatingMsg = true;
            return msgTerm;
        }

        public LogMsg(string message, LogSeverityLvls severity, DateTime timeStamp, int callerLineNum, string callerFilePath, string callerMemberName)
        {
            Message = message;
            Severity = severity;
            TimeStamp = timeStamp;
            CallerLineNumber = callerLineNum;
            CallerFilePath = callerFilePath;
            CallerMemberName = callerMemberName;
        }

        private LogMsg()
        {
        }
    }
}
