using System;

namespace Shuttle.Esb.Tools.DumpMessages
{
    public static class ColoredConsole
    {
        private static readonly object Padlock = new();

        public static void WriteLine(ConsoleColor color, string format, params object[] arg)
        {
            lock (Padlock)
            {
                var foregroundColor = Console.ForegroundColor;

                Console.ForegroundColor = color;
                Console.WriteLine(format, arg);
                Console.ForegroundColor = foregroundColor;
            }
        }
    }
}