using System;

namespace Crux
{
    class Log
    {
        /// <summary>
        /// Log level
        /// 0: Error messages
        /// 1: Important messages
        /// 2: Normal messages
        /// 3: Debug messages
        /// </summary>
        public static int LogLevel = 3;

        public static void Write(string msg, int level)
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
            }
        }
    }
}
