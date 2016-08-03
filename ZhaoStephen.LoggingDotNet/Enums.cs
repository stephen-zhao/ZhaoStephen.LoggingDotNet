namespace ZhaoStephen.LoggingDotNet
{
    public enum LogSeverityLvls
    {
        OFF = 0,
        FATAL = 1,
        ERROR = 2,
        WARN = 4,
        INFO = 8,
        DEBUG = 16,
        ALL = 31
    }

    public enum LogOrnamentLvl
    {
        OFF = 0,
        SIMPLIFIED = 1,
        REDUCED = 2,
        STANDARD = 4,
        INCREASED = 8,
        FULL = 16
    }
}