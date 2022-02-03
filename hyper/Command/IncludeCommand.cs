using hyper.Command;
using hyper.commands;
using hyper.config;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using ZWave.BasicApplication.Devices;

namespace hyper
{
    public class IncludeCommand : BaseCommand
    {
        private readonly Controller controller;
        private readonly List<ConfigItem> configList;
        private readonly string profile;

        public static Regex GetRegex(string oneTo255Regex)
        {
            return new Regex(@$"^include\s*([a-zA-Z_]+)?");
        }

        private bool abort = false;

        public bool Active { get; private set; } = false;

        private ICommand currentCommand = null;

        public IncludeCommand(Controller controller, List<ConfigItem> configList, string profile)
        {
            this.controller = controller;
            this.configList = configList;
            this.profile = profile;
        }

        public override bool Start()
        {
            Active = true;
            Common.logger.Info("-----------");
            Common.logger.Info("Inclusion mode");
            Common.logger.Info("-----------");

            Common.logger.Info("Starting inclusion, please wake up device...");

            var nodeIncluded = false;
            byte nodeId = 0;
            do
            {
                nodeIncluded = Common.IncludeNode(controller, out nodeId);
                if (!nodeIncluded)
                {
                    Common.logger.Warn("Could not include any node, trying again...");
                    Thread.Sleep(200);
                }
            } while (!nodeIncluded && !abort);

            if (abort)
            {
                Common.logger.Info("aborted!");
                return false;
            }
            Common.logger.Info("Success! New node id: {0}", nodeId);

            Common.logger.Info("Inclusion done!");

            currentCommand = new ConfigCommand(controller, nodeId, configList, false, profile);
            return currentCommand.Start();
        }

        public static string GetProfile(string input, Regex regex)
        {
            return regex.Match(input).Groups[1].Value;
        }

        public override void Stop()
        {
            Common.logger.Info("aborting... Please wait.");
            abort = true;
            currentCommand?.Stop();
        }
    }
}