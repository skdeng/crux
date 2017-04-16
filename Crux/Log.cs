using System;
using System.IO;
using System.Threading.Tasks;

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
            Task.Run(() =>
            {
                if (level <= LogLevel)
                {
                    switch (level)
                    {
                        case 0:
                            {
                                var prevColor = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(msg);
                                Console.ForegroundColor = prevColor;
                                break;
                            }
                        case 1:
                            {
                                var prevColor = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(msg);
                                Console.ForegroundColor = prevColor;
                                break;
                            }
                        case 2:
                        case 3:
                            {
                                Console.WriteLine(msg);
                                break;
                            }
                    }

                    FileWriter?.WriteLine($"[{level}] {msg}");
                    FileWriter?.Flush();
                }
            });
        }
    }
}
