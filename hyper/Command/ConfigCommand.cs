﻿using hyper.Command;
using hyper.commands;
using hyper.config;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZWave.BasicApplication.Devices;

namespace hyper
{
    public class ConfigCommand : BaseCommand
    {
        private static Regex regex = new Regex(@$"^(config|readconfig)\s*({OneTo255Regex})\s*({ProfileRegex})?\s*(!)?");

        private readonly Controller controller;
        private readonly List<ConfigItem> configList;
        private readonly string profile;
        private readonly bool readconfig;
        private bool abort = false;

        public bool Active { get; private set; } = false;

        public static bool IsMatch(string command)
        {
            return regex.IsMatch(command);
        }

        public static bool IsReadConfig(string command)
        {
            return "readconfig".Equals(regex.Match(command).Groups[1].Value);
        }

        public static byte GetNodeId(string command)
        {
            var val = regex.Match(command).Groups[2].Value;
            return byte.Parse(val);
        }

        public static bool IsRetry(string command)
        {
            return regex.Match(command).Groups[4].Value == "!";
        }

        /// <summary>
        /// Get the profile, including an optional parameter
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string GetProfile(string command)
        {
            return regex.Match(command).Groups[3].Value;
        }

        public ConfigCommand(Controller controller, byte nodeId, List<ConfigItem> configList, bool retry = false,
            string profile = "", bool readconfig = false)
        {
            this.controller = controller;
            NodeId = nodeId;
            this.configList = configList;
            this.Retry = retry;
            this.profile = profile;
            this.readconfig = readconfig;
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
            ConfigItem config = Common.GetConfigurationForDevice(controller, NodeId, configList, profile, ref abort);
            if (config == null)
            {
                Common.logger.Info("could not find configuration!");
                Common.logger.Info("Either there is no configuration or device did not reply!");
                Common.logger.Error(errorMsg);
                Active = false;
                return false;
            }

            Common.logger.Info("configuration found for {0}!", config.deviceName);
            if (string.IsNullOrEmpty(config.profile))
            {
                Common.logger.Info("using default configuration profile");
            }
            else
            {
                Common.logger.Info($"using configuration profile {config.profile}");
            }
            if (readconfig)
            {
                errorMsg = $"Read configuration failed for node {NodeId}!";
                if (Common.ReadConfiguration(controller, NodeId, config, ref abort))
                {
                    Common.logger.Info($"Read configuration successful for node {NodeId}!");
                    Common.logger.Info("-------------------");
                    Active = false;
                    return true;
                }
            }
            else
            {
                Common.logger.Info("Setting values.");
                if (Common.SetConfiguration(controller, NodeId, config, ref abort))
                {
                    Common.logger.Info($"Configuration successful for node {NodeId}!");
                    Common.logger.Info("-------------------");
                    Active = false;
                    return true;
                }

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