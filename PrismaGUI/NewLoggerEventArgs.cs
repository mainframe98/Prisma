using System;
using Serilog;

namespace PrismaGUI
{
    internal class NewLoggerEventArgs : EventArgs
    {
        public LoggerConfiguration LoggerConfiguration { get; }

        public NewLoggerEventArgs(LoggerConfiguration logConfig)
        {
            this.LoggerConfiguration = logConfig;
        }
    }
}
