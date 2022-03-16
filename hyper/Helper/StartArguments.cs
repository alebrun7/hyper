using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace hyper.Helper
{
    public class StartArguments
    {
        public const string AutoPort = "auto";
        public const string SimulatePort = "simulate";

        public StartArguments(string[] args)
        {
            var remainingArgs = args;
            if (remainingArgs.Length == 0)
            {
                Port = AutoPort;
                Valid = true;
            }
            if ((remainingArgs.Length > 0) && remainingArgs[0].ToLower() == "-udpmultiplexer")
            {
                StartUdpMultiplexer = true;
                remainingArgs = remainingArgs.Skip(1).ToArray();
            }
            if ((remainingArgs.Length > 0) && !IsNextArgAnOption(remainingArgs)) {
                Valid = true;
                Port = remainingArgs[0];
                remainingArgs = remainingArgs.Skip(1).ToArray();
            }
            if (IsNextArgAnOption(remainingArgs))
            {
                Valid = false;
            }
            if (Valid)
            {
                Command = string.Join(" ", remainingArgs);
            }
        }

        private bool IsNextArgAnOption(string[] remainingArgs)
        {
            return (remainingArgs.Length > 0) && remainingArgs[0].StartsWith("-");
        }

        public bool Valid { get; private set; }
        public string Port { get; private set; }
        public bool StartUdpMultiplexer { get; private set; }
        public string Command { get; private set; }

        public void PrintUsage()
        {
            Common.logger.Info("usage:");
            Common.logger.Info("./hyper [-udpmultiplexer] serialPort [command]");
            Common.logger.Info("valid commands:");
            //just refactored from Program.cs, but not up to date:
            Common.logger.Info("r/replace, c/config, i/include, e/exclude, l/listen, p/ping");
        }

    }
}
