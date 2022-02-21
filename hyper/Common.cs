﻿using hyper.config;
using hyper.Helper;
using hyper.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Tasks;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;

namespace hyper
{
    public class Common
    {
        public static TransmitOptions txOptions = TransmitOptions.TransmitOptionAcknowledge | TransmitOptions.TransmitOptionAutoRoute | TransmitOptions.TransmitOptionExplore;
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static bool InitControllerAuto(bool startUdpMultiplexer, out Controller controller, out string errorMessage)
        {
            controller = null;
            errorMessage = string.Empty;
            var detectedPorts = SerialPort.GetPortNames();
            if (detectedPorts.Length > 0)
            {
                Common.logger.Info("Detected serial ports: {0}", string.Join(" ", detectedPorts));
            }
            else 
            { 
                errorMessage = "No serial ports detected";
                return false;
            }
            var portsToTry = detectedPorts.ToList();
            string lastportfilename = "lastport.txt";
            if (File.Exists(lastportfilename))
            {
                string lastport = File.ReadAllText(lastportfilename);
                if (detectedPorts.Contains(lastport))
                {
                    Common.logger.Info("Initialize Serialport: trying last sucessfull port {0} first", lastport);
                    portsToTry.Remove(lastport);
                    portsToTry.Insert(0, lastport);
                }
            }

            foreach (string port in portsToTry)
            {
                bool initialized = Common.InitController(port, startUdpMultiplexer, out controller, out errorMessage);
                if (initialized)
                {
                    File.WriteAllText(lastportfilename, port);
                    return true;
                }
            }
            return false;
        }

        public static bool InitController(string port, bool startUdpMultiplexer, out Controller controller, out string errorMessage)
        {
            IDataSource dataSource = new SerialPortDataSource(port, BaudRates.Rate_115200);
            SerialPortTransportLayer transportLayer = new SerialPortTransportLayer();

            BasicApplicationLayer AppLayer = new BasicApplicationLayer(
                new SessionLayer(),
                new BasicFrameLayer(),
                transportLayer);
            var _controller = AppLayer.CreateController();
            var controllerConnected = _controller.Connect(dataSource);
            if (controllerConnected != CommunicationStatuses.Done)
            {
                for (int i = 2; i < 5; i++)
                {
                    Common.logger.Info("Serialport - connect: try {0}", i);
                    controllerConnected = _controller.Connect(dataSource);
                    if (controllerConnected == CommunicationStatuses.Done)
                    {
                        break;
                    }
                }
            }
            if (controllerConnected == CommunicationStatuses.Done)
            {
                //   Common.logger.Info("connection done!");
                var versionRes = _controller.GetVersion();
                if (!versionRes)
                {
                    var i = 2;
                    while (true)
                    {
                        Common.logger.Info("Controller - get version: try {0}", i);
                        versionRes = _controller.GetVersion();
                        if (versionRes)
                        {
                            break;
                        }
                        i++;
                    }
                }
                if (versionRes)
                {
                    //     Common.logger.Info("connection success!!");
                    //  Common.logger.Info(versionRes.Version);
                    _controller.GetPRK();
                    _controller.SerialApiGetInitData();
                    _controller.SerialApiGetCapabilities();
                    _controller.GetControllerCapabilities();
                    _controller.GetSucNodeId();
                    _controller.MemoryGetId();
                    //      Common.logger.Info("Initialization done!");
                    //  Common.logger.Info("Included Nodes: " + string.Join(", ", controller.IncludedNodes));
                }
                else
                {
                    //   Common.logger.Info("could not get version...");
                    errorMessage = "Could not communicate with controller.";
                    _controller.Disconnect();
                    _controller.Dispose();
                    controller = null;
                    return false;
                }
            }
            else
            {
                // Common.logger.Info("could not connect to " + port);
                errorMessage = string.Format("Could not connect to port {0}. Reason: {1}", port, controllerConnected);
                _controller.Disconnect();
                _controller.Dispose();
                controller = null;
                return false;
            }
            controller = _controller;
            errorMessage = "";

            if (startUdpMultiplexer)
            {
                var multiplexer = new UDPMultiplexer(transportLayer.TransportClient);
                multiplexer.Start();
            }

            return true;
        }

        public static ConfigItem GetConfigurationForDevice(Controller controller, byte nodeId, List<ConfigItem> configList,
            string profile, ref bool abort)
        {
            var retryCount = 3;
            var gotDeviceIds = GetManufactor(controller, nodeId, out int manufacturerId, out int productTypeId, out int productId);
            while (!gotDeviceIds && retryCount >= 0 && !abort)
            {
                Common.logger.Info("could not get device data! Trying again, wake up device!");
                gotDeviceIds = GetManufactor(controller, nodeId, out manufacturerId, out productTypeId, out productId);
                retryCount--;
            }
            if (!gotDeviceIds)
            {
                Common.logger.Info("Too many retrys, aborting!");
                return null;
            }

            var config = GetConfigurationForDevice(
                configList, manufacturerId, productTypeId, productId, profile);
            return config;
        }

        public static ConfigItem GetConfigurationForDevice(List<ConfigItem> configList,
            int manufacturerId, int productTypeId, int productId, string profileWithParam = null)
        {
            const string DefaultProfile = "default";
            string paramValue = "";
            string profile = DefaultProfile;
            if (!string.IsNullOrEmpty(profileWithParam))
            {
                profile = RemoveParamFromProfileName(profileWithParam, out paramValue);
            }
            var foundList = configList.FindAll(item =>
                    item.manufacturerId == manufacturerId
                    && item.productTypeId == productTypeId
                    && (item.productId == productId || item.productId == 0)
                );

            ConfigItem config = null;
            config = foundList.Find(item => profile.Equals(item.profile));
            if (config == null && foundList.Count > 0)
            {
                config = foundList.Find(item => DefaultProfile.Equals(item.profile) || string.IsNullOrEmpty(item.profile));
            }
            config = ReplaceParamValueInConfig(paramValue, config);
            return config;
        }

        /// <summary>
        /// Make a deep clone of the config item. We need it to change some values without touching the original list.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static ConfigItem CloneConfigItem(ConfigItem config)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yaml = serializer.Serialize(config);

            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            return deserializer.Deserialize<ConfigItem>(yaml);
        }

        private static string RemoveParamFromProfileName(string profileWithParam, out string param)
        {
            var split = profileWithParam.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                param = split[1];
            }
            else
            {
                param = "";
            }
            return split[0];
        }

        private static ConfigItem ReplaceParamValueInConfig(string param, ConfigItem config)
        {
            if (config != null && !String.IsNullOrEmpty(param)) {
                config = CloneConfigItem(config);
                foreach (var key in config.groups.Keys.ToArray<byte>())
                {
                    config.groups[key] = String.Format(config.groups[key], param);
                }
            }
            return config;
        }

        public static bool GetManufactor(Controller controller, byte nodeId, out int manufacturerId, out int productTypeId, out int productId)
        {
            var cmd = new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_GET();
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT(), 5000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT)result.Command;
                manufacturerId = Tools.GetInt32(rpt.manufacturerId);
                Common.logger.Info("ManufacturerId: {0} (0x{0:X})",manufacturerId);
                productId = Tools.GetInt32(rpt.productId);
                Common.logger.Info("ProductId: {0} (0x{0:X})", productId);
                productTypeId = Tools.GetInt32(rpt.productTypeId);
                Common.logger.Info("ProductTypeId: {0} (0x{0:X})", productTypeId);
                return true;
            }
            else
            {
                manufacturerId = 0;
                productTypeId = 0;
                productId = 0;
                return false;
            }
        }

        public static string GetProfiles(List<ConfigItem> configList)
        {
            ICollection<String> profiles = new SortedSet<String>();
            configList.FindAll(c => !string.IsNullOrEmpty(c.profile))
                .ForEach(c => profiles.Add(c.profile));
            return String.Join(" ", profiles);
        }

        public static bool ReadConfiguration(Controller controller, byte nodeId, ConfigItem config, ref bool abort)
        {
            bool differenceFound = false;
            if (config.groups != null)
            {
                foreach (var group in config.groups)
                {
                    var list = GetAssociationsForGroup(controller, nodeId, group.Key);
                    if (list == null)
                    {
                        return false;
                    }
                    var expected = GetAssociationsFromConfigGroup(group);
                    var missing = expected.Except(list);
                    var tooMuch = list.Except(expected);
                    if (!missing.IsNullOrEmpty() || !tooMuch.IsNullOrEmpty())
                    {
                        differenceFound = true;
                    }
                }
            }
            if (config.config != null && config.config.Count != 0)
            {
                Common.logger.Info("Reading " + config.config.Count + " configuration parameter");
                foreach (var entry in config.config)
                {
                    var configParameter = byte.Parse(entry.Key.Split("_")[0]);
                    int value = 0;
                    if (ReadParameter(controller, nodeId, configParameter, ref value))
                    {
                        logger.Info($"node {nodeId}, param {configParameter}: {value}");
                        if (value != entry.Value)
                        {
                            differenceFound = true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (config.wakeup != 0)
            {
                if (config.wakeup != GetWakeUp(controller, nodeId))
                {
                    differenceFound = true;
                }
            }
            if (differenceFound)
            {
                logger.Warn($"node {nodeId}: configuration does not match");
            }
            return true;
        }

            public static bool SetConfiguration(Controller controller, byte nodeId, ConfigItem config, ref bool abort)
        {
            int retryCount = 3;
            if (config.groups != null && config.groups.Count != 0)
            {
                if (config.groups.Count != 0)
                {
                    Common.logger.Info("Setting " + config.groups.Count + " associtions");
                    var associationCleared = false;
                    do
                    {
                        associationCleared = ClearAssociations(controller, nodeId);
                        if (!associationCleared)
                        {
                            Common.logger.Info("Not successful! Trying again, please wake up device.");
                            Thread.Sleep(200);
                            retryCount--;
                        }
                    } while (!associationCleared && !abort && retryCount > 0);

                    if (retryCount <= 0 || abort)
                    {
                        Common.logger.Info("Too many retrys or aborted!");
                        return false;
                    }
                }

                var associations = ExpandAssociationList(config);
                foreach (var group in associations)
                {
                    var groupIdentifier = group.Key;

                    var member = group.Value;

                    retryCount = 3;
                    var associationValidated = false;
                    do
                    {
                        var associationAdded = AddAssociation(controller, nodeId, groupIdentifier, member);
                        if (associationAdded)
                        {
                            associationValidated = AssociationContains(controller, nodeId, groupIdentifier, member);
                        }
                        if (!associationValidated)
                        {
                            Common.logger.Info("Not successful! Trying again, please wake up device.");
                            Thread.Sleep(200);
                            retryCount--;
                        }
                    } while (!associationValidated && !abort && retryCount > 0);

                    if (retryCount <= 0 || abort)
                    {
                        Common.logger.Info("Too many retrys or aborted!");
                        return false;
                    }

                    //var associationAdded = AddAssociation(controller, nodeId, groupIdentifier, member);
                    //var associationValidated = false;
                    //if (associationAdded)
                    //{
                    //    associationValidated = AssociationContains(controller, nodeId, groupIdentifier, member);
                    //}

                    //while (!associationAdded || !associationValidated)
                    //{
                    //    Common.logger.Info("Not successful! Trying again, please wake up device.");
                    //    associationAdded = AddAssociation(controller, nodeId, groupIdentifier, member);
                    //    if (associationAdded)
                    //    {
                    //        associationValidated = AssociationContains(controller, nodeId, groupIdentifier, member);
                    //    }
                    //    retryCount--;
                    //    if (retryCount <= 0 || abort)
                    //    {
                    //        Common.logger.Info("Too many retrys or aborted!");
                    //        return false;
                    //    }
                    //    Thread.Sleep(200);
                    //}
                    retryCount = 3;
                }
            }

            if (config.config != null && config.config.Count != 0)
            {
                Common.logger.Info("Setting " + config.config.Count + " configuration parameter");
                foreach (var configurationEntry in config.config)
                {
                    var configParameter = configurationEntry.Key;
                    var configValue = configurationEntry.Value;

                    bool parameterValidated = false;
                    do
                    {
                        var parameterSet = SetParameter(controller, nodeId, configParameter, configValue);
                        if (parameterSet)
                        {
                            parameterValidated = ValidateParameter(controller, nodeId, configParameter, configValue);
                        }
                        if (!parameterValidated)
                        {
                            Common.logger.Info("Not successful! Trying again, please wake up device.");
                            Thread.Sleep(200);
                            retryCount--;
                        }
                    } while (!parameterValidated && !abort && retryCount > 0);

                    if (retryCount <= 0 || abort)
                    {
                        Common.logger.Info("Too many retrys or aborted!");
                        return false;
                    }

                    //var parameterSet = SetParameter(controller, nodeId, configParameter, configValue);
                    //var parameterValidated = false;
                    //if (parameterSet)
                    //{
                    //    parameterValidated = ValidateParameter(controller, nodeId, configParameter, configValue);
                    //}
                    //while (!parameterSet || !parameterValidated)
                    //{
                    //    Common.logger.Info("Not successful! Trying again, please wake up device.");
                    //    parameterSet = SetParameter(controller, nodeId, configParameter, configValue);
                    //    if (parameterSet)
                    //    {
                    //        parameterValidated = ValidateParameter(controller, nodeId, configParameter, configValue);
                    //    }
                    //    retryCount--;
                    //    if (retryCount <= 0 || abort)
                    //    {
                    //        Common.logger.Info("Too many retrys or aborted!");
                    //        return false;
                    //    }
                    //}
                    Thread.Sleep(200);
                }
            }
            if (config.wakeup != 0)
            {
                retryCount = 3;
                GetWakeUp(controller, nodeId);
                bool intervalValidated = false;
                do
                {
                    var wakeupSet = SetWakeUp(controller, nodeId, config.wakeup);
                    if (wakeupSet)
                    {
                        intervalValidated = ValidateWakeUp(controller, nodeId, config);
                        if (!intervalValidated)
                        {
                            Common.logger.Info("Not successful! Trying again, please wake up device.");
                            Thread.Sleep(200);
                            retryCount--;
                        }
                    }
                } while (!intervalValidated && !abort && retryCount > 0);

                if (retryCount <= 0 || abort)
                {
                    Common.logger.Info("Too many retrys or aborted!");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Allows for multiple nodes for each group. Transforms the groups Dictionary to a list of individual associations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static List<KeyValuePair<byte, byte>> ExpandAssociationList(ConfigItem config)
        {
            var associations = new List<KeyValuePair<byte, byte>>();
            foreach (var group in config.groups)
            {
                var values = GetAssociationsFromConfigGroup(group);
                foreach (var value in values)
                {
                    associations.Add(new KeyValuePair<byte, byte>(group.Key, value));
                }
            }
            return associations;
        }

        private static IList<byte> GetAssociationsFromConfigGroup(KeyValuePair<byte, string> group)
        {
            List<byte> nodes = new List<byte>();
            char[] separators = new char[] { ' ', ',' };
            var values = group.Value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var value in values)
            {
                byte member = 0;
                if (byte.TryParse(value, out member))
                {
                    if (member != 0)
                    {
                        nodes.Add(member);
                    }
                }
            }
            nodes.Sort();
            return nodes;
        }

        private static bool ValidateWakeUp(Controller controller, byte nodeId, ConfigItem config)
        {
            int actualInterval = GetWakeUp(controller, nodeId);
            if (config.wakeup == actualInterval)
            {
                Common.logger.Info("parameter ist set correctly!");
                return true;
            }
            else
            {
                if (actualInterval != -1)
                {
                    Common.logger.Warn("wake up interval has value {0} instead of {1}", actualInterval, config.wakeup);
                }
                return false;
            }
        }

        public static bool SetWakeUp(Controller controller, byte nodeId, int configValue)
        {
            Common.logger.Info("Set Wakeup - value " + configValue);
            var cmd = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_SET
            {
                nodeid = 1, //node to send the wake up notification, for us always controller with id 1
                seconds = Tools.GetBytes(configValue).Skip(1).ToArray()
            };
            Common.logger.Debug($"value is [{cmd.seconds[0]},{cmd.seconds[1]},{cmd.seconds[2]}]");
            var setWakeup = controller.SendData(nodeId, cmd, Common.txOptions);
            return setWakeup.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static int GetWakeUp(Controller controller, byte nodeId)
        {
            var cmd = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_GET();
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_REPORT(), 20000);
            int seconds = -1; // -1 means error here. value cannot be -1 because it is only 3 bytes
            if (result)
            {
                var rpt = (COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_REPORT)result.Command;
                seconds = Tools.GetInt32(rpt.seconds);
                Common.logger.Info("wake up interval: {0}, nodeid (to send notifications): {1}",
                    seconds, rpt.nodeid);
            }
            else
            {
                Common.logger.Warn($"Could not get wake up interval!!");
            }
            return seconds;
        }

        public static bool GetWakeUpCapabilities(Controller controller, byte nodeId)
        {
            var cmd = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_GET();
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_REPORT(), 20000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_REPORT)result.Command;
                logger.Info("wake up minimumWakeUpIntervalSeconds: " + Tools.GetInt32(rpt.minimumWakeUpIntervalSeconds));
                logger.Info("wake up maximumWakeUpIntervalSeconds: " + Tools.GetInt32(rpt.maximumWakeUpIntervalSeconds));
                logger.Info("wake up defaultWakeUpIntervalSeconds: " + Tools.GetInt32(rpt.defaultWakeUpIntervalSeconds));
                logger.Info("wake up wakeUpIntervalStepSeconds: " + Tools.GetInt32(rpt.wakeUpIntervalStepSeconds));
            }
            else
            {
                Common.logger.Warn("Could Not get wake up capabilities!!");
            }
            return result;
        }

        public static bool SetParameter(Controller controller, byte nodeId, string configParameterLong, int configValue)
        {
            var configParameter = byte.Parse(configParameterLong.Split("_")[0]);
            var configWordSize = byte.Parse(configParameterLong.Split("_")[1]);

            Common.logger.Info("Set configuration - parameter " + configParameter + " - value " + configValue);
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET cmd = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET();
            cmd.parameterNumber = configParameter;
            if (configWordSize == 1)
            {
                cmd.configurationValue = new byte[] { (byte)configValue };
            }
            else if (configWordSize == 2)
            {
                cmd.configurationValue = Tools.GetBytes((ushort)configValue);
            }
            else
            {
                cmd.configurationValue = Tools.GetBytes(configValue);
            }
            cmd.properties1.mdefault = 0;
            cmd.properties1.size = configWordSize;

            var setAssociation = controller.SendData(nodeId, cmd, Common.txOptions);
            return setAssociation.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool ValidateParameter(Controller controller, byte nodeId, string configParameterLong, int configValue)
        {
            var configParameter = byte.Parse(configParameterLong.Split("_")[0]);
            Common.logger.Info("Validate configuration - parameter " + configParameter + " - value " + configValue);
            int value = 0;

            if (ReadParameter(controller, nodeId, configParameter, ref value))
            {
                if (configValue == value)
                {
                    Common.logger.Info("parameter ist set correctly!");
                    return true;
                }
                else
                {
                    Common.logger.Info("parametr has value {0} instead of {1}", value, configValue);
                    return false;
                }
            }
            else
            {
                Common.logger.Info("Could not get configuration parameter!");
                return false;
            }
        }

        static bool ReadParameter(Controller controller, byte nodeId, byte configParameter, ref int value)
        {
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET cmd = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET();
            cmd.parameterNumber = configParameter;
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT(), 20000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT)result.Command;
                value = Tools.GetInt32(rpt.configurationValue.ToArray());
                return true;
            }
            return false;
        }

        public static bool AssociationContains(Controller controller, byte nodeId, byte groupIdentifier, byte member)
        {
            Common.logger.Info("Validate associtation - group " + groupIdentifier + " - node " + member);

            var associatedNodes = GetAssociationsForGroup(controller, nodeId, groupIdentifier);
            if (associatedNodes != null)
            {
                if (associatedNodes.Contains(member))
                {
                    Common.logger.Info(member + " is a member of association group " + groupIdentifier);
                    return true;
                }
                else
                {
                    Common.logger.Info(member + " is not a member of association group " + groupIdentifier);
                    return false;
                }
            }
            else
            {
                Common.logger.Info("could not get association for group " + groupIdentifier);
                return false;
            }
        }

        private static IList<byte> GetAssociationsForGroup(Controller controller, byte nodeId, byte groupIdentifier)
        {
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET();
            cmd.groupingIdentifier = groupIdentifier;
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT(), 20000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT)result.Command;
                Common.logger.Info("members for node {0}, group {1}: {2}", nodeId, groupIdentifier, String.Join(", ", rpt.nodeid));
                return rpt.nodeid;
            }
            return null;
        }

        public static bool AddAssociation(Controller controller, byte nodeId, byte groupIdentifier, byte member)
        {
            Common.logger.Info("Add associtation - group " + groupIdentifier + " - node " + member);
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET();
            cmd.groupingIdentifier = groupIdentifier;
            cmd.nodeId = new List<byte>() { member };
            var setAssociation = controller.SendData(nodeId, cmd, Common.txOptions);
            return setAssociation.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        private static bool ClearAssociations(Controller controller, byte nodeId)
        {
            Common.logger.Info("Clear associtations - group  for node " + nodeId);
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE();
            cmd.groupingIdentifier = 0;
            cmd.nodeId = new byte[] { 0 };
            var clearAssociation = controller.SendData(nodeId, cmd, Common.txOptions);
            return clearAssociation.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool SetBinary(Controller controller, byte nodeId, bool value)
        {
            Common.logger.Info("SWITCH_BINARY_SET for node {0}: {1}", nodeId, value);
            var cmd = new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_SET()
            {
                targetValue = Convert.ToByte(value)
            };
            var setBasic = controller.SendData(nodeId, cmd, txOptions);
            return setBasic.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool GetBinary(Controller controller, byte nodeId, out bool value)
        {
            Common.logger.Info("Binary_Get for node {0}", nodeId);
            var cmd = new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_GET();
            var result = controller.RequestData(nodeId, cmd, txOptions, new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT(), 10000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT)result.Command;
                value = Convert.ToBoolean(rpt.currentValue);
                return true;
            }
            value = false;
            return false;
        }

        public static bool SetBasic(Controller controller, byte nodeId, bool value)
        {
            Common.logger.Info("Basic_Set for node {0}: {1}", nodeId, value);
            var cmd = new COMMAND_CLASS_BASIC_V2.BASIC_SET
            {
                value = Convert.ToByte(value)
            };

            var setBasic = controller.SendData(nodeId, cmd, txOptions);
            return setBasic.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool SetMulti(Controller controller, byte nodeId, byte endpoint, bool value)
        {
            Common.logger.Info("Multi for node {0}: {1} {2}", nodeId, endpoint, value);
            var cmd = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP();
            cmd.properties1.sourceEndPoint = endpoint;
            cmd.properties2.destinationEndPoint = endpoint;
            cmd.commandClass = 32;
            cmd.command = 1;
            cmd.parameter = value ? new byte[] { 255 } : new byte[] { 0 };

            var setBasic = controller.SendData(nodeId, cmd, txOptions);
            return setBasic.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool GetBasic(Controller controller, byte nodeId, out bool value)
        {
            Common.logger.Info("Basic_Get for node {0}", nodeId);
            var cmd = new COMMAND_CLASS_BASIC_V2.BASIC_GET();
            var result = controller.RequestData(nodeId, cmd, txOptions, new COMMAND_CLASS_BASIC_V2.BASIC_REPORT(), 10000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_BASIC_V2.BASIC_REPORT)result.Command;
                value = Convert.ToBoolean(rpt.currentValue);
                return true;
            }
            value = false;
            return false;
        }

        internal static bool ThermostatSetpoint(Controller controller, byte nodeId, float value)
        {
            Common.logger.Info("ThermostatSetpoint node {0} with value {1}", nodeId, value);
            var cmd = new COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_SET();
            const byte Heating = 1;
            const byte Celcius = 0; //Fahrenheit = 1;
            cmd.properties1.setpointType = Heating;

            cmd.properties2.precision = 1;
            cmd.properties2.scale = Celcius;
            cmd.properties2.size = 2; //short
            ushort valueShort = (ushort)(value * 10f); //precision 1
            cmd.value = Tools.GetBytes(valueShort);

            var sendDataResult = controller.SendData(nodeId, cmd, txOptions);
            return sendDataResult.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool WriteNVRam(Controller controller, byte[] eeprom)
        {
            if (eeprom.Length != 65536)
            {
                Common.logger.Warn("Wrong file size!");
                return false;
            }
            int counter = 0;
            while (counter < 65536)
            {
                if (counter % 4096 == 0)
                {
                    Common.logger.Info("Progress: {0}", (counter / 65536f).ToString("0.00%"));
                }

                var result = controller.WriteNVRam((ushort)counter, 128, eeprom.Skip(counter).Take(128).ToArray());
                if (result.State == ZWave.ActionStates.Completed)
                {
                    //  Buffer.BlockCopy(result.RetValue, 0, eeprom, counter, 128);
                }
                else
                {
                    Common.logger.Error("error reading NVRam");
                    return false;
                }
                counter += 128;
            }
            Common.logger.Info("Progress: {0}", (65536f / 65536f).ToString("0.00%"));

            return true;
        }

        public static bool ReadNVRam(Controller controller, out byte[] eeprom)
        {
            eeprom = new byte[65536];
            int counter = 0;
            while (counter < 65536)
            {
                if (counter % 4096 == 0)
                {
                    Common.logger.Info("Progress: {0}", (counter / 65536f).ToString("0.00%"));
                }

                var result = controller.ReadNVRam((ushort)counter, 128);
                if (result.State == ZWave.ActionStates.Completed)
                {
                    Buffer.BlockCopy(result.RetValue, 0, eeprom, counter, 128);
                }
                else
                {
                    Common.logger.Error("error reading NVRam");
                    return false;
                }
                counter += 128;
            }
            Common.logger.Info("Progress: {0}", (65536f / 65536f).ToString("0.00%"));

            return true;
        }

        //public static void DrawTextProgressBar(string stepDescription, int progress, int total)
        //{
        //    int totalChunks = 50;

        //    //draw empty progress bar
        //    Console.CursorLeft = 0;
        //    Console.Write("["); //start
        //    Console.CursorLeft = totalChunks + 1;
        //    Console.Write("]"); //end
        //    Console.CursorLeft = 1;

        //    double pctComplete = Convert.ToDouble(progress) / total;
        //    int numChunksComplete = Convert.ToInt16(totalChunks * pctComplete);

        //    //draw completed chunks
        //    Console.BackgroundColor = ConsoleColor.Green;
        //    Console.Write("".PadRight(numChunksComplete));

        //    //draw incomplete chunks
        //    Console.BackgroundColor = ConsoleColor.Gray;
        //    Console.Write("".PadRight(totalChunks - numChunksComplete));

        //    //draw totals
        //    Console.CursorLeft = totalChunks + 5;
        //    Console.BackgroundColor = ConsoleColor.Black;

        //    string output = String.Format("{0}% of 100%", numChunksComplete * (100d / totalChunks));
        //    Console.Write(output.PadRight(15) + stepDescription); //pad the output so when changing from 3 to 4 digits we avoid text shifting
        //}

        public static bool RequestBatteryReport(Controller controller, byte nodeId)
        {
            //var cmd = new COMMAND_CLASS_BATTERY.BATTERY_GET();
            //var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_BATTERY.BATTERY_REPORT(), 20000);
            //if (result)
            //{
            //    var rpt = (COMMAND_CLASS_BATTERY.BATTERY_REPORT)result.Command;
            //    logger.Info("battery report: " + rpt.batteryLevel);

            //}
            //else
            //{
            //    Common.logger.Info("Could not get battery!!");
            //}
            //return result;

            Common.logger.Info("Request battery value for node {0}", nodeId);
            var cmd = new COMMAND_CLASS_BATTERY.BATTERY_GET();
            var getBattery = controller.SendData(nodeId, cmd, txOptions);
            return getBattery.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static bool ReplaceNode(Controller controller, byte nodeId)
        {
            var replacedNode = controller.ReplaceFailedNode(nodeId, null, Modes.NodeOptionHighPower | Modes.NodeOptionNetworkWide, 20000);
            replacedNode.WaitCompletedSignal();
            var result = (InclusionResult)replacedNode.Result;
            var replaced = result.AddRemoveNode.AddRemoveNodeStatus == ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Replaced;
            if (replaced)
            {
                AddToIncludedNodes(controller, nodeId);
            }
            return replaced;
        }

        public static bool IncludeNode(Controller controller, out byte nodeId)
        {
            var includeNode = controller.IncludeNode(Modes.NodeOptionHighPower | Modes.NodeOptionNetworkWide, 20000);
            nodeId = includeNode.AddRemoveNode.Id;
            var nodeIncluded = includeNode.AddRemoveNode.AddRemoveNodeStatus == ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Added;
            if (nodeIncluded)
            {
                AddToIncludedNodes(controller, nodeId);
            }
            return nodeIncluded;
        }

        public static bool ExcludeNode(Controller controller, out byte nodeId)
        {
            var excludeNode = controller.ExcludeNode(Modes.NodeOptionHighPower | Modes.NodeOptionNetworkWide, 20000);
            nodeId = excludeNode.AddRemoveNode.Id;
            RemoveFromIncludedNodes(controller, nodeId);
            return true;
        }

        private static void AddToIncludedNodes(Controller controller, byte nodeId)
        {
            var list = new SortedSet<byte>(controller.IncludedNodes);
            list.Add(nodeId);
            controller.IncludedNodes = list.ToArray();
        }

        private static void RemoveFromIncludedNodes(Controller controller, byte nodeId)
        {
            var list = new List<byte>(controller.IncludedNodes);
            list.Remove(nodeId);
            controller.IncludedNodes = list.ToArray();
        }

        public static bool MarkNodeFailed(Controller controller, byte nodeId)
        {
            var isFailed = controller.IsFailedNode(nodeId);
            return isFailed.RetValue;
        }

        public static bool CheckReachable(Controller controller, byte nodeId)
        {
            var sendData = controller.SendData(nodeId, new byte[1], Common.txOptions);
            return sendData.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        public static Type GetNestedType(Type baseType, byte id)
        {
            var nestedTypes = baseType.GetNestedTypes(BindingFlags.Public);
            var matchedType = nestedTypes.Where(t =>
            {
                var value = t.GetField("ID")?.GetRawConstantValue();
                if (value == null)
                {
                    return false;
                }

                return (byte)value == id;
            }).FirstOrDefault();
            return matchedType;
        }

        public static Dictionary<byte, Type> GetAllCommandClasses(Assembly assembly, string nameSpace)
        {
            var keySet = new HashSet<byte>();

            var allClasses = assembly.GetTypes().Where(a => a.IsClass && a.Namespace != null && a.Namespace.Contains(nameSpace) && a.Name.StartsWith("COMMAND_CLASS") && !a.Name.Contains("CLASS_ALARM")).OrderBy(x => x.Name, new NaturalSortComparer<string>(false)).Select(t =>
            {
                var constValue = t?.GetField("ID")?.GetRawConstantValue();
                object returnValue = null;
                if (constValue != null && keySet.Add((byte)constValue))
                {
                    returnValue = new KeyValuePair<byte, Type>((byte)constValue, t);//.Name.ToLower());
                }
                return returnValue;
            }).Where(v => v != null).Select(v => (KeyValuePair<byte, Type>)v).ToDictionary(t => t.Key, t => t.Value);

            return allClasses;
        }

        public static Dictionary<Type, Dictionary<byte, Type>> GetAllNestedCommandClasses(ICollection<Type> commandClasses)
        {
            var nestedTypeDict = commandClasses.Select(t =>
            {
                var nestedTypes = t.GetNestedTypes(BindingFlags.Public);
                var dict = nestedTypes.Select(nt =>
                {
                    var id = nt.GetField("ID")?.GetRawConstantValue();
                    object returnVal = null;
                    if (id != null)
                    {
                        returnVal = new KeyValuePair<byte, Type>((byte)id, nt);
                    }
                    return returnVal;
                }).Where(v => v != null).Select(v => (KeyValuePair<byte, Type>)v).ToDictionary(nt => nt.Key, nt => nt.Value);
                return new KeyValuePair<Type, Dictionary<byte, Type>>(t, dict);
            }).ToDictionary(t => t.Key, t => t.Value);
            return nestedTypeDict;
        }

        public static List<ConfigItem> ParseConfig(string configFile)
        {
            var yamlText = File.ReadAllText(configFile);
            //  var input = new StringReader(yamlText);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            List<ConfigItem> configList = null;
            try
            {
                configList = deserializer.Deserialize<List<ConfigItem>>(yamlText);
            }
            catch(YamlException e)
            {
                logger.Error(e.Message);
            }

            return configList;
        }
    }

    //public static int GetValueWithBitmask(int value, int bitmask)
    //{
    //    value = value & bitmask;
    //    int bits = bitmask;
    //    while ((bits & 0x01) == 0)
    //    {
    //        value = value >> 1;
    //        bits = bits >> 1;
    //    }

    //    return value;
    //}

    //public static void GetAssociation(Controller controller, byte nodeId)
    //{
    //    for (byte id = 1; id <= 5; id++)
    //    {
    //        var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET();
    //        cmd.groupingIdentifier = id;
    //        var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT(), 20000);
    //        if (result)
    //        {
    //            var rpt = (COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT)result.Command;
    //            var _groupId = rpt.groupingIdentifier;
    //            if (_groupId != id)
    //            {
    //                continue;
    //            }
    //            var maxSupported = rpt.maxNodesSupported;
    //            var reportToFollow = rpt.reportsToFollow;
    //            var member = string.Join(", ", rpt.nodeid);

    //            Common.logger.Info("Group: {0} - Member: {1} - Max: {2} - Follow: {3}", _groupId, member, maxSupported, reportToFollow);
    //        }
    //    }

    //}

    //public static void GetConfig(Controller controller, byte nodeId)
    //{
    //    for (byte parameterNumber = 1; parameterNumber < 100; parameterNumber++)
    //    {
    //        var cmd = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET();
    //        cmd.parameterNumber = parameterNumber;
    //        RequestDataResult result;
    //        result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT(), 100);
    //        if (result)
    //        {
    //            var rpt = (COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT)result.Command;

    //            var _parameterNumber = rpt.parameterNumber;
    //            if (_parameterNumber != parameterNumber)
    //            {
    //                continue;
    //            }
    //            var value = Tools.GetInt32(rpt.configurationValue.ToArray());

    //            Common.logger.Info("ParameterNumber: {0} - {1}", _parameterNumber, value);
    //        }
    //        else
    //        {
    //            //  Common.logger.Info("Parameter {0} does not exist!", parameterNumber);
    //        }
    //    }

    //}
}