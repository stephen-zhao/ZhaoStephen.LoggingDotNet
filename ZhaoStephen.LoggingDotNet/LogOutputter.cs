﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhaoStephen.LoggingDotNet
{
    public class LogOutputter
    {
        protected static readonly Dictionary<LogOrnamentLvl, string> DEFAULT_DICT_ORNAMENT_FORMATS = new Dictionary<LogOrnamentLvl, string>()
        {
            { LogOrnamentLvl.OFF, "{0}" },
            { LogOrnamentLvl.SIMPLIFIED, "[{1:HH:mm:ss}] {0}" },
            { LogOrnamentLvl.REDUCED, "[{1:HH:mm:ss}] [{2}] {0}" },
            { LogOrnamentLvl.STANDARD, "[{1:yyyy/MM/dd HH:mm:ss}] [{2}] {0}" },
            { LogOrnamentLvl.INCREASED, "[{1:yyyy/MM/dd HH:mm:ss}] [{2}] [{3}] [{4}] {0}" },
            { LogOrnamentLvl.FULL, "[{1:yyyy/MM/dd HH:mm:ss}] [{2}] [{3}] [{4}] [{5}] {0}" }
        };

        public Dictionary<LogOrnamentLvl, string> DictOrnamentFormats { get; protected set; }
        public LogSeverityLvls DisplayedSeverities { get; protected set; }
        public LogOrnamentLvl OrnamentLvl { get; protected set; }
        public TextWriter TextWriter { get; protected set; }
        public bool IsUsingTextWriter { get { return (TextWriter != null); } }
        protected Action<string> WriteAction { get; set; }

        public LogOutputter(TextWriter writer, LogOrnamentLvl ornament, LogSeverityLvls severities)
        {
            DictOrnamentFormats = DEFAULT_DICT_ORNAMENT_FORMATS;
            DisplayedSeverities = severities;
            OrnamentLvl = ornament;
            TextWriter = writer;
            WriteAction = writer.Write;
        }

        public LogOutputter(Action<string> writeAction, LogOrnamentLvl ornament, LogSeverityLvls severities)
        {
            DictOrnamentFormats = DEFAULT_DICT_ORNAMENT_FORMATS;
            DisplayedSeverities = severities;
            OrnamentLvl = ornament;
            WriteAction = writeAction;
        }

        public void Write(LogMsg msg)
        {
            if ((msg.Severity & DisplayedSeverities) == 0)
            {
                return;
            }
            string msgString = String.Format(DictOrnamentFormats[OrnamentLvl],
                msg.Message,
                msg.TimeStamp,
                msg.Severity,
                msg.CallerMemberName,
                msg.CallerLineNumber,
                msg.CallerFilePath) + Environment.NewLine;
            Task.Run(() => WriteAction(msgString));
        }
    }
}