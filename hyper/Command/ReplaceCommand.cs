using hyper.Command;
using hyper.commands;
using hyper.config;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using ZWave.BasicApplication.Devices;

namespace hyper
{
    public class ReplaceCommand : BaseCommand
    {
        private static Regex regex = new Regex(@$"^replace\b\s*({OneTo255Regex})\s*({ProfileRegex})?");

        private readonly Controller controller;
        private readonly byte nodeId;
        private readonly List<ConfigItem> configList;
        private readonly string profile;
        private ICommand currentCommand = null;

        public bool Active { get; private set; } = false;

        private bool abort = false;


        public static bool IsMatch(string input)
        {
            return regex.IsMatch(input);
        }

        public static string GetProfile(string input)
        {
            return regex.Match(input).Groups[2].Value;
        }

        public static byte GetNodeId(string command)
        {
            var val = regex.Match(command).Groups[1].Value;
            return byte.Parse(val);
        }

        public ReplaceCommand(Controller controller, byte nodeId, List<ConfigItem> configList, string profile)
        {
            this.controller = controller;
            this.nodeId = nodeId;
            this.configList = configList;
            this.profile = profile;
        }

        public override bool Start()
        {
            Active = true;
            Common.logger.Info("-----------");
            Common.logger.Info("Replacement mode");
            Common.logger.Info("node to replace: " + nodeId);
            Common.logger.Info("-----------");
            Common.logger.Info("Check if node is reachable...");
            var reachable = Common.CheckReachable(controller, nodeId);
            if (reachable)
            {
                Common.logger.Info("Node is reachable!");
                Common.logger.Info("If node is reachable, we cannot replace it!");
                return false;
            }
            else
            {
                Common.logger.Info("OK, node is not reachable");
            }
            if (abort)
            {
                return false;
            }
            Common.logger.Info("Mark node as failed...");
            var markedAsFailed = Common.MarkNodeFailed(controller, nodeId);
            if (!markedAsFailed)
            {
                Common.logger.Info("Node could not be marked as failed!");
                Common.logger.Info("Try again and ensure that node is not reachable.");
                return false;
            }
            else
            {
                Common.logger.Info("OK, node is marked as failed");
            }
            Common.logger.Info("Replacing Node... Set new device to inclusion mode!");
            bool nodeReplaced = Common.ReplaceNode(controller, nodeId);
            while (!nodeReplaced && !abort)
            {
                Common.logger.Info("Could not replace device! Trying again.");
                Thread.Sleep(200); //same as IncludeCommand. for davert_2 this loop went crazy for 2 devices...
                nodeReplaced = Common.ReplaceNode(controller, nodeId);
            }

            if (abort)
            {
                Common.logger.Info("aborted!");
                return false;
            }

            Common.logger.Info("Node sucessfully replaced!");

            Common.logger.Info("Replacement done!");

            currentCommand = new ConfigCommand(controller, nodeId, configList, false, profile);
            return currentCommand.Start();
        }

         public override void Stop()
        {
            Common.logger.Info("aborting... Please wait.");
            abort = true;
            currentCommand?.Stop();
        }
    }
}