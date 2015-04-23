namespace BaiduHiCrawler
{
    using System;
    using System.IO;

    public class Logger
    {
        #region Private Variables

        private static readonly Logger Instance;

        private LogLevel logLevel;

        private StreamWriter logFileStreamWriter;

        private object logFileStreamWriterLock = new object();

        #endregion

        #region Ctor.

        private Logger()
        {
            this.logLevel = Constants.LogLevel;

            var logFileName = Path.Combine(Directory.GetCurrentDirectory(), Constants.LogsFolder, @"BaiduHiCrawler.log");
            lock (this.logFileStreamWriterLock)
            {
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), Constants.LogsFolder)))
                {
                    Directory.CreateDirectory(Constants.LogsFolder);
                }

                this.logFileStreamWriter = GetLogFileStreamWriter(logFileName);
            }
        }

        static Logger()
        {
            Instance = new Logger();
        }

        #endregion

        #region Public Methods

        public static void SetLogLevel(LogLevel level)
        {
            Instance.logLevel = level;
        }

        public static void LogVerbose(string format, params object[] args)
        {
            Log(LogLevel.Verbose, format, args);
        }

        public static void LogInfo(string format, params object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public static void LogWarning(string format, params object[] args)
        {
            Log(LogLevel.Warning, format, args);
        }

        public static void LogError(string format, params object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public static void Log(LogLevel level, string format, params object[] args)
        {
            if (level < Instance.logLevel)
            {
                return;
            }

            lock (Instance.logFileStreamWriterLock)
            {
                if (Instance.logFileStreamWriter == null)
                {
                    var logFileName = Path.Combine(Directory.GetCurrentDirectory(), @".\Logs\BaiduHiCrawler.log");

                    var streamWriter = GetLogFileStreamWriter(logFileName);
                    if (streamWriter == null)
                    {
                        return;
                    }
                    Instance.logFileStreamWriter = streamWriter;
                }

                var logText = string.Format(format, args);
                Instance.logFileStreamWriter.WriteLine("[{0} | {1}] {2}", DateTime.UtcNow.ToString("u"), level, logText);
                Instance.logFileStreamWriter.Flush();
            }
        }

        #endregion

        #region Private Methods

        private static StreamWriter GetLogFileStreamWriter(string logFileName)
        {
            try
            {
                var logFileStream = new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                var logFileStreamWriter = new StreamWriter(logFileStream);
                return logFileStreamWriter;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
