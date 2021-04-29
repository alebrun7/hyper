using hyper.Command;
using hyper.commands;
using hyper.config;
using System.Collections.Generic;
using ZWave.BasicApplication.Devices;

namespace hyper
{
    public class ConfigCommand : BaseCommand
    {
        private readonly Controller controller;

        private readonly List<ConfigItem> configList;

        private bool abort = false;

        public bool Active { get; private set; } = false;

        public ConfigCommand(Controller controller, byte nodeId, List<ConfigItem> configList, bool retry = false)
        {
            this.controller = controller;
            NodeId = nodeId;
            this.configList = configList;
            this.Retry = retry;
        }

        public override bool Start()
        {
            Active = true;
            Common.logger.Info("-----------");
            Common.logger.Info("Configuration mode");
            Common.logger.Info("node to configure: " + NodeId);
            Common.logger.Info("-----------");

            string errorMsg = $"Configuration failed for node {NodeId}!";

            Common.logger.Info("Getting configuration for device...");
            ConfigItem config = Common.GetConfigurationForDevice(controller, NodeId, configList, ref abort);
            if (config == null)
            {
                Common.logger.Info("could not find configuration!");
                Common.logger.Info("Either there is no configuration or device did not reply!");
                Common.logger.Error(errorMsg);
                Active = false;
                return false;
            }

            Common.logger.Info("configuration found for {0}!", config.deviceName);
            Common.logger.Info("Setting values.");
            if (Common.SetConfiguration(controller, NodeId, config, ref abort))
            {
                Common.logger.Info($"Configuration successful for node {NodeId}!");
                Common.logger.Info("-------------------");
                Active = false;
                return true;
            }
            Common.logger.Error(errorMsg);
            return false;
        }

        public override void Stop()
        {
            Common.logger.Info("aborting... Please wait.");
            abort = true;
            // Common.logger.Warn("Cannot abort!");
        }
    }
}