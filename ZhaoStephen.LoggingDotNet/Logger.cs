using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZhaoStephen.LoggingDotNet
{
    public class Logger : IDisposable
    {
        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Defaults (Readonly)                                                             ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Default Readonly Fields
        private const int DEFAULT_CALLER_LINE_NUMBER = 0;
        private const string DEFAULT_CALLER_FILE_PATH = "UnknownCallerFilePath";
        private const string DEFAULT_CALLER_MEMBER_NAME = "UnknownCallerMemberName";
        public static readonly string DEFAULT_LOG_DIR = "";
        public static readonly string DEFAULT_LOG_MAIN_FILENAME = "program_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
        public static readonly LogOrnamentLvl DEFAULT_MAIN_LOG_ORNAMENT = LogOrnamentLvl.FULL;
        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Static Members                                                                  ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Static Members
        public static string LogDir { get; set; } = DEFAULT_LOG_DIR;
        public static string MainLogFileName { get; set; } = DEFAULT_LOG_MAIN_FILENAME;
        public static string MainLogFile { get { return Path.Combine(LogDir, MainLogFileName); } }
        private static List<Logger> _instances;
        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Instance Members                                                                ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Instance Members
        public string Name { get; set; }
        private BlockingCollection<LogMsg> MsgQueue { get; set; }
        private Thread MsgThread { get; set; }
        private List<LogOutputter> ListOutputs { get; set; }
        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Static Methods                                                                  ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Static Setup Methods

        public static void SetupLogging(string dirLogRoot = "", string mainLogFileName = "")
        {
            // Fill in static members from parameters
            if (!String.IsNullOrWhiteSpace(dirLogRoot))
            {
                LogDir = dirLogRoot;
            }
            if (!String.IsNullOrWhiteSpace(mainLogFileName))
            {
                MainLogFileName = mainLogFileName;
            }
            // Do other static logger setup
        }

        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Public Constructors                                                             ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Constructor

        public Logger(string name)
        {
            Name = name;

            ListOutputs = new List<LogOutputter>();
            ListOutputs.Add(new LogOutputter(new StreamWriter(new FileStream(MainLogFile, FileMode.Create, FileAccess.Write)), LogOrnamentLvl.FULL, LogSeverityLvls.ALL));
            
            MsgQueue = new BlockingCollection<LogMsg>(new ConcurrentQueue<LogMsg>());
            
            StartMsgThread();
        }

        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Public Logging Methods                                                          ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Logging Methods

        public void Fatal(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.FATAL, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }

        public void Error(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.ERROR, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }

        public void Warn(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.WARN, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }

        public void Info(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.INFO, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }

        public void Debug(string message, 
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER, 
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH, 
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.DEBUG, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }

        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Public Output Setup Methods                                                     ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Setup Output Methods

        public void AddOutputStream(StreamWriter writer, LogOrnamentLvl ornament, LogSeverityLvls severities=LogSeverityLvls.ALL)
        {
            ListOutputs.Add(new LogOutputter(writer, ornament, severities));
        }

        public void AddOutputGeneric(Action<string> writeAction, LogOrnamentLvl ornament, LogSeverityLvls severities=LogSeverityLvls.ALL)
        {
            ListOutputs.Add(new LogOutputter(writeAction, ornament, severities));
        }

        public void AddOutputControl(Control control, LogOrnamentLvl ornament, LogSeverityLvls severities=LogSeverityLvls.ALL)
        {
            if (control is TextBox)
            {
                ListOutputs.Add(new LogOutputter(str => control.Invoke(new Action(() => ((TextBox)control).AppendText(str))), ornament, severities));
            }
            else if (control is ListBox)
            {
                ListOutputs.Add(new LogOutputter(str => control.Invoke(new Action(() => ((ListBox)control).Items.Add(str))), ornament, severities));
            }
            else if (control is Label)
            {
                ListOutputs.Add(new LogOutputter(str => control.Invoke(new Action(() => ((Label)control).Text += str)), ornament, severities));
            }
            else
            {
                throw new NotSupportedException("Windows Forms Controls of type " + control.GetType() + "are not supported by LoggingDotNet.");
            }
        }

        public void ClearOutputList()
        {
            ListOutputs.Clear();
        }

        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Private Thread Methods                                                          ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////

        private void EnqueueMsg(string message, LogSeverityLvls severity, DateTime timeStamp, int callerLineNum, string callerFilePath, string callerMemberName)
        {
            LogMsg msg = new LogMsg(message, severity, timeStamp, callerLineNum, callerFilePath, callerMemberName);
            MsgQueue.Add(msg);
        }

        private void StartMsgThread()
        {
            MsgThread = new Thread(MsgThreadRun);
            MsgThread.Name = Name + ".MsgThread";
            MsgThread.IsBackground = true;
            MsgThread.Start();
        }

        private void MsgThreadRun()
        {
            while (true)
            {
                LogMsg msg = MsgQueue.Take();
                if (msg.IsThreadTerminatingMsg) break;
                foreach(LogOutputter outputter in ListOutputs)
                {
                    outputter.Write(msg);
                }
            }
        }

        private void TerminateMsgThread()
        {
            MsgQueue.Add(LogMsg.GetThreadTerminatingMsg());
        }


        protected virtual void Dispose(bool disposing)
        {
            TerminateMsgThread();
            if (disposing)
            {
                MsgQueue.Dispose();
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
