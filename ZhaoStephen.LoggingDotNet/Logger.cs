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
    /// <summary>
    /// The class that does the logging.
    /// 
    /// </summary>
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
        protected static readonly string DEFAULT_LOG_DIR = "";
        protected static readonly string DEFAULT_LOG_MAIN_FILENAME = "program_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
        protected static readonly LogOrnamentLvl DEFAULT_MAIN_LOG_ORNAMENT_LVL = LogOrnamentLvl.FULL;
        protected static readonly LogSeverityLvls DEFAULT_MAIN_LOG_SEVERITY_LVLS = LogSeverityLvls.ALL;
        #endregion






        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Static Members                                                                  ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Static Members
        

        /// <summary>
        /// The static directory where log files are placed.
        /// </summary>
        public static string LogDir { get; protected set; } = DEFAULT_LOG_DIR;


        /// <summary>
        /// The name of the main log file.
        /// </summary>
        public static string MainLogFileName { get; protected set; } = DEFAULT_LOG_MAIN_FILENAME;


        /// <summary>
        /// The full path to the main log file.
        /// </summary>
        public static string MainLogFile { get { return Path.Combine(LogDir, MainLogFileName); } }


        /// <summary>
        /// The ornament level of the main log file.
        /// </summary>
        public static LogOrnamentLvl MainLogOrnamentLvl { get; protected set; } = DEFAULT_MAIN_LOG_ORNAMENT_LVL;


        /// <summary>
        /// The severity levels of the main log file.
        /// </summary>
        public static LogSeverityLvls MainLogSeverityLvls { get; protected set; } = DEFAULT_MAIN_LOG_SEVERITY_LVLS;


        /// <summary>
        /// The number of instances of Logger running.
        /// </summary>
        public static int InstanceCount { get { return _instances.Count; } }


        /// <summary>
        /// Whether or not for the program to do logging in a main log file.
        /// </summary>
        public static bool DoMainLogging { get; protected set; } = true;
        

        private static bool _isDoneStaticSetup = false;
        private static List<Logger> _instances;


        #endregion


        



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Instance Members                                                                ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Instance Members


        /// <summary>
        /// The name of the Logger instance.
        /// </summary>
        public string Name { get; set; }

        
        /// <summary>
        /// List of outputters for this Logger.
        /// </summary>
        public List<LogOutputter> ListOutputs { get; protected set; }


        private BlockingCollection<LogMsg> MsgQueue { get; set; }
        private Thread MsgThread { get; set; }


        #endregion






        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Static Methods                                                                  ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Static Methods


        /// <summary>
        /// The Logger static constructor.
        /// </summary>
        static Logger()
        {
            _instances = new List<Logger>();
        }


        /// <summary>
        /// The Logger static setup method. 
        /// Use this to configure static properties before instantiating any Loggers.
        /// </summary>
        /// <param name="dirLogRoot">Root directory for generated log files.</param>
        /// <param name="mainLogFileName">Name of the main log file.</param>
        /// <param name="mainLogOrnamentLvl">Ornament level of the main log file.</param>
        /// <param name="mainLogSeverityLvls">Severity levels shown in the main log file.</param>
        /// <param name="doMainLogging">Whether or not for Loggers to also log to a static main log file.</param>
        public static void SetupLogging(string dirLogRoot="", 
            string mainLogFileName="", 
            LogOrnamentLvl mainLogOrnamentLvl=0,
            LogSeverityLvls mainLogSeverityLvls=0,
            bool doMainLogging=true)
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
            if (mainLogOrnamentLvl != 0)
            {
                MainLogOrnamentLvl = mainLogOrnamentLvl;
            }
            if (mainLogSeverityLvls != 0)
            {
                MainLogSeverityLvls = mainLogSeverityLvls;
            }
            if (!doMainLogging)
            {
                DoMainLogging = doMainLogging;
            }
            // Set the setup complete flag
            _isDoneStaticSetup = true;
        }


        #endregion






        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Public Constructors                                                             ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Constructor

       
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the Logger instance.</param>
        /// <param name="outputToMainLog">Whether or not for the instance to also output to the main log file.</param>
        public Logger(string name, bool outputToMainLog=true)
        {
            // Check the setup complete flag
            if (!_isDoneStaticSetup)
            {
                throw new Exception("Static Logger has not been set up. Use \"Logger.SetupLogging(...)\" to setup the Logger class.");
            }

            // Set name of Logger
            Name = name;

            // Instantiate list of outputs
            ListOutputs = new List<LogOutputter>();

            // If appropriate, add the main log file to the list of outputs
            if (outputToMainLog && DoMainLogging)
            {
                ListOutputs.Add(new LogOutputter(new StreamWriter(new FileStream(MainLogFile, FileMode.OpenOrCreate, FileAccess.Write)), MainLogOrnamentLvl, MainLogSeverityLvls));
            }

            // Instantiate the message queue
            MsgQueue = new BlockingCollection<LogMsg>(new ConcurrentQueue<LogMsg>());
            
            // Start the message listener and ouput thread
            StartMsgThread();
            
            // Add the instance to a list of instances
            _instances.Add(this);
        }

        #endregion



        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Public Logging Methods                                                          ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Public: Logging Methods


        /// <summary>
        /// Logs a FATAL message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="callerLineNum"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerMemberName"></param>
        public void Fatal(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.FATAL, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }


        /// <summary>
        /// Logs an ERROR message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="callerLineNum"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerMemberName"></param>
        public void Error(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.ERROR, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }


        /// <summary>
        /// Logs a WARN message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="callerLineNum"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerMemberName"></param>
        public void Warn(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.WARN, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }


        /// <summary>
        /// Logs an INFO message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="callerLineNum"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerMemberName"></param>
        public void Info(string message,
            [CallerLineNumber] int callerLineNum = DEFAULT_CALLER_LINE_NUMBER,
            [CallerFilePath] string callerFilePath = DEFAULT_CALLER_FILE_PATH,
            [CallerMemberName] string callerMemberName = DEFAULT_CALLER_MEMBER_NAME)
        {
            EnqueueMsg(message, LogSeverityLvls.INFO, DateTime.Now, callerLineNum, callerFilePath, callerMemberName);
        }


        /// <summary>
        /// Logs a DEBUG message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="callerLineNum"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerMemberName"></param>
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
        
        
        /// <summary>
        /// Adds a TextWriter as an output destination for the Logger.
        /// </summary>
        /// <param name="writer">The TextWriter to output with.</param>
        /// <param name="ornament">The ornament level of output to this destination.</param>
        /// <param name="severities">The severity levels allowed in the output to this destination.</param>
        public void AddOutputWriter(TextWriter writer, LogOrnamentLvl ornament, LogSeverityLvls severities=LogSeverityLvls.ALL)
        {
            ListOutputs.Add(new LogOutputter(writer, ornament, severities));
        }


        /// <summary>
        /// Adds a custom action as an output method for the Logger.
        /// </summary>
        /// <param name="writeAction">An action that operates on the log message string.</param>
        /// <param name="ornament">The ornament level of output to this destination.</param>
        /// <param name="severities">The severity levels allowed in the output to this destination.</param>
        public void AddOutputGeneric(Action<string> writeAction, LogOrnamentLvl ornament, LogSeverityLvls severities=LogSeverityLvls.ALL)
        {
            ListOutputs.Add(new LogOutputter(writeAction, ornament, severities));
        }


        /// <summary>
        /// Adds a windows forms Control as an output destination for the Logger.
        /// This version only supports TextBox, ListBox, and Label.
        /// </summary>
        /// <param name="control">The Control to output to.</param>
        /// <param name="ornament">The ornament level of output to this destination.</param>
        /// <param name="severities">The severity levels allowed in the output to this destination.</param>
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


        /// <summary>
        /// Clears the list of output destinations.
        /// </summary>
        public void ClearOutputList()
        {
            ListOutputs.Clear();
        }


        #endregion






        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   IDisposable Methods                                                             ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region IDisposable Methods


        protected virtual void Dispose(bool disposing)
        {
            TerminateMsgThread();
            if (disposing)
            {
                MsgQueue.Dispose();
            }
            _instances.Remove(this);
        }


        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }


        #endregion






        ///////////////////////////////////////////////////////////////////////////////////////////
        ////                                                                                   ////
        ////   Private Thread Methods                                                          ////
        ////                                                                                   ////
        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Private: Thread Methods


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


        #endregion


    }
}