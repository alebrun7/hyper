using hyper.Helper;
using hyper.Helper.Extension;
using System.Text.RegularExpressions;
using ZWave.CommandClasses;

namespace hyper.Command
{
    public class SimulateHelper
    {
        private Match match;

        public static Regex GetSimulateRegex(string oneTo255Regex)
        {
            return new Regex(@$"^simulate\s+({oneTo255Regex})\s+(bin|bw|mk)\s+(false|true)"); //simulate 3 mk true => tür auf
        }

        public SimulateHelper(Regex simulateRegex, string simulateVal)
        {
            match = simulateRegex.Match(simulateVal);
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
    }
}
