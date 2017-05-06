using System;

namespace Crux
{
    class ConsoleLogStream : ILogStream
    {
        public void Write(string msg, int level)
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
