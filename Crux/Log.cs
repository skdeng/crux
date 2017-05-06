using System;
using System.IO;

namespace Crux
{
    public class Log
    {
        /// <summary>
        /// Log level
        /// 0: Error messages
        /// 1: Important messages
        /// 2: Normal messages
        /// 3: Debug messages
        /// </summary>
        public static int LogLevel = 3;

        public static ILogStream LogStream = new ConsoleLogStream();

        private static string _LogExportFile;
        public static string LogExportFile
        {
            get { return _LogExportFile; }
            set
            {
                _LogExportFile = value;
                if (FileWriter == null)
                {
                    FileWriter = File.AppendText(_LogExportFile);
                    FileWriter.AutoFlush = true;
                }
            }
        }

        private static StreamWriter FileWriter = null;

        public static void CloseLogFile()
        {
            FileWriter.Close();
        }

        public static void Write(string msg, int level)
        {
            if (level <= LogLevel)
            {
                var formatMsg = $"[{level}] [{DateTime.Now.ToString("yy/MM/dd hh:mm:ss")}] {msg}";
                LogStream.Write(formatMsg, level);
                FileWriter?.WriteLineAsync(formatMsg);
            }
        }
    }
}
