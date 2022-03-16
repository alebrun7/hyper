using hyper.config;
using hyper.Helper;
using hyper.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            Common.logger.Info("Controller - Connected to Port: {0}", port);
            return true;
        }

        public static ConfigItem GetConfigurationForDevice(Controller controller, byte nodeId, List<ConfigItem> configList,
            string profile, ref bool abort)
        {
            var retryCount = 3;
            ConfigItem config = null;
            PerformWithRetries(ref retryCount, ref abort, () =>
            {
                bool gotDeviceIds = GetManufactor(controller, nodeId,
                    out int manufacturer, out int productType, out int product);
                if (gotDeviceIds)
                {
                    config = GetConfigurationForDevice(configList, manufacturer, productType, product, profile);
                }
                return gotDeviceIds;
            });
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
            logger.Info($"Get device data for node {nodeId}...");
            var cmd = new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_GET();
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT(), 5000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT)result.Command;
                manufacturerId = Tools.GetInt32(rpt.manufacturerId);
                Common.logger.Info("ManufacturerId: {0} (0x{0:X})", manufacturerId);
                productId = Tools.GetInt32(rpt.productId);
                Common.logger.Info("ProductId: {0} (0x{0:X})", productId);
                productTypeId = Tools.GetInt32(rpt.productTypeId);
                Common.logger.Info("ProductTypeId: {0} (0x{0:X})", productTypeId);

                SendDeviceSpecificGet(controller, nodeId);
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

        /// <summary>
        /// Get Device SeriaNumber, if supported by the device
        /// Do not wait for the response. If the device answers, the information
        /// will be logged in ListenCommand
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="nodeId"></param>
        public static void SendDeviceSpecificGet(Controller controller, byte nodeId)
        {
            logger.Info($"Send DEVICE_SPECIFIC_GET to node {nodeId}");
            var cmd = new COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_GET();
            cmd.properties1.deviceIdType = 0;
            controller.SendData(nodeId, cmd, Common.txOptions);
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
            GetVersion(controller, nodeId, ref abort);
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
                    if (expected == null)
                    {
                        return false;
                    }
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
            GetVersion(controller, nodeId, ref abort);
            bool configured = ConfigureAssociations(controller, nodeId, config, ref abort);
            configured = configured && ConfigureParameters(controller, nodeId, config, ref abort);
            return configured && ConfigureWakeup(controller, nodeId, config, ref abort);
        }

        enum LibraryType : byte
        {
            StaticController = 1,
            Controller = 2,
            EnhancedSlave = 3,
            Slave = 4,
            Installer = 5,
            RoutingSlave = 6,
            BridgeController = 7,
            DeviceUnderTest = 8,
            AV_Remote = 0x0A,
            AV_Device = 0x0B,
        }

        internal static void GetVersion(Controller controller, byte nodeId, ref bool abort)
        {
            int retryCount = 3;
            PerformWithRetries(ref retryCount, ref abort, () =>
            {
                var cmd = new COMMAND_CLASS_VERSION.VERSION_GET();
                var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_VERSION.VERSION_REPORT(), 5000);
                if (result)
                {
                    var rpt = (COMMAND_CLASS_VERSION.VERSION_REPORT)result.Command;
                    string libraryTypeName = "";
                    if (Enum.IsDefined(typeof(LibraryType), rpt.zWaveLibraryType))
                    {
                        libraryTypeName = "(" + ((LibraryType)rpt.zWaveLibraryType).ToString() + ")";
                    }
                    Common.logger.Info($"version for node {nodeId}: "
                        + $"zWaveLibraryType={rpt.zWaveLibraryType}{libraryTypeName}, "
                        + $"zWaveProtocolVersion={rpt.zWaveProtocolVersion}.{rpt.zWaveProtocolSubVersion}, "
                        + $"applicationVersion={rpt.applicationVersion}.{rpt.applicationSubVersion}");
                    return true;
                }
                return false;
            });
        }

        public static bool ConfigureAssociations(Controller controller, byte nodeId, ConfigItem config, ref bool abort)
        {
            int retryCount = 3;
            if (config.groups != null && config.groups.Count != 0)
            {
                Common.logger.Info("Setting " + config.groups.Count + " associations");
                foreach (var group in config.groups)
                {
                    var groupIdentifier = group.Key;
                    IList<byte> currentAssociations = null;
                    PerformWithRetries(ref retryCount, ref abort, () => {
                        currentAssociations = GetAssociationsForGroup(controller, nodeId, groupIdentifier);
                        return currentAssociations != null;
                    });
                    if (currentAssociations == null)
                    {
                        return false;
                    }
                    var expected = GetAssociationsFromConfigGroup(group);
                    if (expected == null)
                    {
                        return false;
                    }
                    var missing = expected.Except(currentAssociations);
                    var tooMuch = currentAssociations.Except(expected);
                    if (!tooMuch.IsNullOrEmpty())
                    {
                        bool removed = PerformWithRetries(ref retryCount, ref abort, () =>
                        {
                            return RemoveAssociations(controller, nodeId, groupIdentifier, tooMuch.ToList());
                        });
                        if (!removed)
                        {
                            return false;
                        }
                    }
                    if (!missing.IsNullOrEmpty())
                    {
                        bool added = PerformWithRetries(ref retryCount, ref abort, () =>
                        {
                            return AddAssociations(controller, nodeId, groupIdentifier, missing.ToList());
                        });
                        if (!added)
                        {
                            return false;
                        }
                    }
                    if (missing.IsNullOrEmpty() && tooMuch.IsNullOrEmpty())
                    {
                        logger.Info($"node {nodeId}, group {groupIdentifier}: nothing to do");
                    }
                    else
                    {
                        bool validated = PerformWithRetries(ref retryCount, ref abort, () =>
                        {
                            currentAssociations = GetAssociationsForGroup(controller, nodeId, groupIdentifier);
                            if (currentAssociations == null)
                            {
                                return false;
                            }
                            missing = expected.Except(currentAssociations);
                            tooMuch = currentAssociations.Except(expected);
                            return (missing.IsNullOrEmpty() && tooMuch.IsNullOrEmpty());
                        });
                        if (!validated)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Performs a function with some retries if it fails
        /// </summary>
        /// <param name="retryCount">Number of retries left</param>
        /// <param name="abort">Reference to a variable to allow aborting the loop</param>
        /// <param name="myFunction">The function to performs. Must return true upon success,
        /// false if it fails</param>
        static bool PerformWithRetries(ref int retryCount, ref bool abort, Func<bool> myFunction)
        {
            bool success = false;
            do
            {
                success = myFunction();
                if (!success)
                {
                    Common.logger.Info("Not successful! Trying again, please wake up device.");
                    Thread.Sleep(200);
                    retryCount--;
                }

            } while (!success && !abort && retryCount > 0);
            if (retryCount <= 0 || abort)
            {
                Common.logger.Info("Too many retrys or aborted!");
                return false;
            }
            return true;
        }

        public static bool ConfigureParameters(Controller controller, byte nodeId, ConfigItem config, ref bool abort)
        {
            int retryCount = 3;
            if (config.config != null && config.config.Count != 0)
            {
                Common.logger.Info("Setting " + config.config.Count + " configuration parameter");
                foreach (var configurationEntry in config.config)
                {
                    var configParameter = configurationEntry.Key;
                    var configValue = configurationEntry.Value;

                    bool setAndValidated = PerformWithRetries(ref retryCount, ref abort, () =>
                    {
                        var parameterSet = SetParameter(controller, nodeId, configParameter, configValue);
                        if (parameterSet)
                        {
                            return ValidateParameter(controller, nodeId, configParameter, configValue);
                        }
                        return false;
                    });

                    if (!setAndValidated)
                    {
                        return false;
                    }
                    Thread.Sleep(200);
                }
            }
            return true;
        }

        public static bool ConfigureWakeup(Controller controller, byte nodeId, ConfigItem config, ref bool abort)
        {
            if (config.wakeup != 0)
            {
                int retryCount = 3;
                int actual = GetWakeUp(controller, nodeId);
                if (actual == config.wakeup)
                {
                    logger.Info($"node {nodeId}: noting to do for wake up interval");
                }
                else
                {
                    return PerformWithRetries(ref retryCount, ref abort, () =>
                    {
                        var wakeupSet = SetWakeUp(controller, nodeId, config.wakeup);
                        if (wakeupSet)
                        {
                            return ValidateWakeUp(controller, nodeId, config);
                        }
                        return false;
                    });
                }
            }
            return true;
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
                else
                {
                    if (value == "{0}")
                    {
                        logger.Error($"Missing Parameter for the configuration profile");
                    }
                    else
                    {
                        logger.Error($"The value {value} is not a valid node id");
                    }
                    return null;
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

        private static IList<byte> GetAssociationsForGroup(Controller controller, byte nodeId, byte group)
        {
            logger.Info($"Get associations for node {nodeId}, group {group}...");
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET();
            cmd.groupingIdentifier = group;
            var result = controller.RequestData(nodeId, cmd, Common.txOptions, new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT(), 20000);
            if (result)
            {
                var rpt = (COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT)result.Command;
                Common.logger.Info("members for node {0}, group {1}: {2}", nodeId, group, String.Join(", ", rpt.nodeid));
                return rpt.nodeid;
            }
            return null;
        }

        public static bool AddAssociations(Controller controller, byte nodeId, byte group, IList<byte> nodes)
        {
            string list = String.Join(", ", nodes);
            logger.Info($"Add associations for node {nodeId}, group {group}: {list}...");
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET();
            cmd.groupingIdentifier = group;
            cmd.nodeId = nodes;
            var setAssociation = controller.SendData(nodeId, cmd, Common.txOptions);
            return setAssociation.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        private static bool RemoveAssociations(Controller controller, byte nodeId, byte group, IList<byte> nodes)
        {
            string list = String.Join(", ", nodes);
            logger.Info($"Remove associations for node {nodeId}, group {group}: {list}...");
            var cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE();
            cmd.groupingIdentifier = group;
            cmd.nodeId = nodes;
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

        internal static bool RequestThermostatMode
            (Controller controller, byte nodeId)
        {
            Common.logger.Info("Get Thermostat Mode for node {0}", nodeId);
            var cmd = new COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_GET();
            var sendDataResult = controller.SendData(nodeId, cmd, txOptions);
            return sendDataResult.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        internal static bool ThermostatMode(Controller controller, byte nodeId, byte value)
        {
            Common.logger.Info("Set Thermostat Mode for node {0} to value {1}", nodeId, value);
            var cmd = new COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET();
            cmd.properties1.mode = value;

            var sendDataResult = controller.SendData(nodeId, cmd, txOptions);
            return sendDataResult.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        internal static bool RequestThermostatSetpoint(Controller controller, byte nodeId)
        {
            Common.logger.Info("Get Thermostat Setpoint for node {0}", nodeId);
            var cmd = new COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_GET();
            const byte Heating = 1;
            cmd.properties1.setpointType = Heating;
            var sendDataResult = controller.SendData(nodeId, cmd, txOptions);
            return sendDataResult.TransmitStatus == TransmitStatuses.CompleteOk;
        }

        internal static bool ThermostatSetpoint(Controller controller, byte nodeId, float value)
        {
            const byte Heating = 1;
            const byte Celcius = 0; //Fahrenheit = 1;
            ThermostatMode(controller, nodeId, Heating);
            Common.logger.Info("Set Thermostat Setpoint for node {0} to value {1}", nodeId, value);
            var cmd = new COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_SET();
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
            logger.Info("ExclusionResult: State={0}", excludeNode.State);
            RemoveFromIncludedNodes(controller, nodeId);
            return excludeNode.IsStateCompleted;
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