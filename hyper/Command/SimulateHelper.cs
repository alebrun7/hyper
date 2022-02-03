using hyper.Helper;
using hyper.Helper.Extension;
using System.Text.RegularExpressions;
using ZWave.CommandClasses;

namespace hyper.Command
{
    public class SimulateHelper
    {
        private static Regex simulateRegex = new Regex(@$"^simulate\s+({BaseCommand.OneTo255Regex})\s+(bin|bw|ft|mk)\s+(false|true)"); //simulate 3 mk true => tür auf
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
            var value = bool.Parse(match.Groups[3].Value);

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
