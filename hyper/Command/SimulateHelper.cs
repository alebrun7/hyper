using hyper.Helper;
using hyper.Helper.Extension;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZWave.CommandClasses;

namespace hyper.Command
{
    public class SimulateHelper
    {
        //z.B. "simulate 3 mk true" => door open
        //"simulate 4 t 1 true" => button 1 on for touch panel
        private static Regex simulateRegex = new Regex(
            @$"^simulate\s+({BaseCommand.OneTo255Regex})\s+(bin|bw|ft|mk|t)\s*(1|2)?\s*(false|true)");
        private static Regex simulateOnOffRegex = new Regex(@$"^simulate\s+(false|true)"); //simulate true => simulation mode on

        private Match match;
        private bool hasController;

        public static bool MatchesSimulate(string simulateVal)
        {
            return simulateRegex.IsMatch(simulateVal);
        }

        public static bool MatchesSimulateOnOff(string command)
        {
            return simulateOnOffRegex.IsMatch(command);
        }

        public SimulateHelper(string simulateVal, object controller)
        {
            if (simulateRegex.IsMatch(simulateVal)) {
                match = simulateRegex.Match(simulateVal);
            }
            else
            {
                match = simulateOnOffRegex.Match(simulateVal);
            }
            hasController = (controller != null);
        }

        public byte NodeId { get; private set;}
        public object Command { get; private set; }

        public void CreateCommand()
        {
            NodeId = byte.Parse(match.Groups[1].Value);
            var type = match.Groups[2].Value; //bin,bw,mk
            var endpointStr = match.Groups[3].Value;
            var value = bool.Parse(match.Groups[4].Value);

            //command is always lower case
            switch (type)
            {
                case "bin":
                case "ft":
                    Command = new COMMAND_CLASS_BASIC_V2.BASIC_SET()
                    {
                        value = value ? (byte)255 : (byte)0

                    };
                    break;
                case "bw":
                    //there is a COMMAND_CLASS_NOTIFICATION_V8:NOTIFICATION_REPORT too,
                    //but only SENSOR_BINARY_REPORT is send to alfred
                    Command = new COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT()
                    {
                        sensorValue = value ? (byte)255 : (byte)0
                    };
                    break;
                case "mk":
                    Command = new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT()
                    {
                        notificationType = (byte)NotificationType.AccessControl,
                        mevent = value ? (byte)AccessControlEvent.WindowDoorIsOpen : (byte)AccessControlEvent.WindowDoorIsClosed
                    };
                    break;
                case "t": //TPS412
                    Command = CreateMultiChannelBasicReportEncap(endpointStr, value);
                    break;
                default:
                    Common.logger.Error($"simulate: type {type} not recognized!");
                    break;
            }
            if (Command != null)
            {
                Command.GetKeyValue(out Enums.EventKey eventKey, out float eventValue);
                Common.logger.Info($"simulate event - id: {NodeId} - key: {eventKey} - value: {eventValue}");
            }
        }

        public static COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP CreateMultiChannelBasicReportEncap(string endpointStr, bool value)
        {
            byte endpoint = byte.Parse(endpointStr);
            //sends 3 Messages:
            /*
            2022-03-14 16:41:27.7835 INFO 03/14/2022 16:41:27: COMMAND_CLASS_BASIC_V2:BASIC_SET from node 42
            2022-03-14 16:41:27.7835 INFO id: 42 - key: BASIC - value: 1
            2022-03-14 16:41:27.8018 INFO 03/14/2022 16:41:27: COMMAND_CLASS_BASIC_V2:BASIC_REPORT from node 42
            2022-03-14 16:41:27.8018 INFO id: 42 - key: STATE_ON - value: 1
            2022-03-14 16:41:27.8086 INFO same message or too soon! doing nothing
            2022-03-14 16:41:27.8086 INFO But different key: BASIC - STATE_ON
            2022-03-14 16:41:27.8211 INFO 03/14/2022 16:41:27: COMMAND_CLASS_MULTI_CHANNEL_V4:MULTI_CHANNEL_CMD_ENCAP from node 42
            2022-03-14 16:41:27.8211 INFO id: 42 - key: CHANNEL_1_STATE - value: 1
            */
            // {"properties1":{"sourceEndPoint":1,"res":0},"properties2":{"destinationEndPoint":1,"bitAddress":0},"commandClass":32,"command":3,"parameter":[255]}
            var cmd = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
            {
                commandClass = COMMAND_CLASS_BASIC_V2.ID,
                command = COMMAND_CLASS_BASIC_V2.BASIC_REPORT.ID,
                parameter = new List<byte>() { value ? (byte)255 : (byte)0 },
            };
            cmd.properties1.sourceEndPoint = endpoint;
            cmd.properties1.res = 0;
            cmd.properties2.destinationEndPoint = endpoint;
            cmd.properties2.bitAddress = 0;
            return cmd;
        }

        public bool GetSimulationMode()
        {
            if (match.Groups.Count == 2)
            {
                bool mode = bool.Parse(match.Groups[1].Value);
                if (hasController || mode)
                {
                    return mode;
                }
                else
                {
                    Common.logger.Warn("No Controller, cannot stop Simulation Mode.");
                    return true;
                }
            }
            return false;
        }
    }
}
