using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;
using ZWave.CommandClasses;

namespace hyper.Tests.Command
{
    [TestClass]
    public class SimulateHelperTest
    {
        object dummyController = new object();

        [TestMethod]
        public void GetSimulateRegexTest()
        {
            for (int nodeId = 0; nodeId <= 1000; ++nodeId)
            {
                bool expected = 0 < nodeId && nodeId < 256;
                string cmd = $"simulate {nodeId} bw true";
                Assert.AreEqual(expected, SimulateHelper.MatchesSimulate(cmd), $"IsMatch should be {expected} for cmd={cmd}");
            }
        }

        [TestMethod]
        public void SimulateBin()
        {
            var helper = new SimulateHelper("simulate 3 bin true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            var basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(255, basic_set.value);
            Assert.AreEqual(3, helper.NodeId);

            helper = new SimulateHelper("simulate 3 bin false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(0, basic_set.value);
        }

        [TestMethod]
        public void SimulateBw()
        {
            var helper = new SimulateHelper("simulate 3 bw true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT), helper.Command.GetType());
            var report = (COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT)helper.Command;
            Assert.AreEqual(255, report.sensorValue);

            helper = new SimulateHelper("simulate 3 bw false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT), helper.Command.GetType());
            report = (COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT)helper.Command;
            Assert.AreEqual(0, report.sensorValue);
        }

        [TestMethod]
        public void SimulateFt()
        {
            var helper = new SimulateHelper("simulate 3 ft true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            var basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(255, basic_set.value);
            Assert.AreEqual(3, helper.NodeId);

            helper = new SimulateHelper("simulate 3 ft false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(0, basic_set.value);
        }

        [TestMethod]
        public void SimulateMk()
        {
            var helper = new SimulateHelper("simulate 3 mk true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT), helper.Command.GetType());
            var report = (COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT)helper.Command;
            Assert.AreEqual((byte)NotificationType.AccessControl, report.notificationType);
            Assert.AreEqual((byte)AccessControlEvent.WindowDoorIsOpen, report.mevent);

            helper = new SimulateHelper("simulate 3 mk false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT), helper.Command.GetType());
            report = (COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT)helper.Command;
            Assert.AreEqual((byte)NotificationType.AccessControl, report.notificationType);
            Assert.AreEqual((byte)AccessControlEvent.WindowDoorIsClosed, report.mevent);
        }

        [TestMethod]
        public void SimulateT_Channel1_On()
        {
            var helper = new SimulateHelper("simulate 3 t 1 true", dummyController);
            helper.CreateCommand();

            CheckMultiChannelCommand(helper.Command, 1, true);
        }
        [TestMethod]
        public void SimulateT_Channel1_Off()
        {
            var helper = new SimulateHelper("simulate 3 t 1 false", dummyController);
            helper.CreateCommand();

            CheckMultiChannelCommand(helper.Command, 1, false);
        }
        [TestMethod]
        public void SimulateT_Channel2_On()
        {
            var helper = new SimulateHelper("simulate 3 t 2 true", dummyController);
            helper.CreateCommand();

            CheckMultiChannelCommand(helper.Command, 2, true);
        }
        [TestMethod]
        public void SimulateT_Channel2_Off()
        {
            var helper = new SimulateHelper("simulate 3 t 2 false", dummyController);
            helper.CreateCommand();

            CheckMultiChannelCommand(helper.Command, 2, false);
        }

        [TestMethod]
        public void SimulateScene_Scene1to4_Recognized()
        {
            for (int sceneNumber = 1; sceneNumber < 5; ++sceneNumber)
            {
                string command = $"simulate 3 scene {sceneNumber}";
                Assert.IsTrue(SimulateHelper.MatchesSimulate(command));
            }
        }

        [TestMethod]
        public void SimulateScene_Scene1to4_CommandGenerated()
        {
            for (int sceneNumber = 1; sceneNumber < 5; ++sceneNumber)
            {
                string command = $"simulate 3 scene {sceneNumber}";
                Assert.IsTrue(SimulateHelper.MatchesSimulate(command));

                var helper = new SimulateHelper(command, dummyController);
                helper.CreateCommand();

                CheckCentralSceneCommand(helper.Command, sceneNumber);
            }
        }

        [TestMethod]
        public void SimulateBattery_Recognized()
        {
            foreach (byte value in BatteryValues())
            {
                string command = $"simulate 3 battery {value}";
                Assert.IsTrue(SimulateHelper.MatchesSimulate(command));

                var helper = new SimulateHelper(command, dummyController);
                helper.CreateCommand();
                CheckBatteryCommand(helper.Command, value);
            }
        }


        [TestMethod]
        public void SimulateWakeup_Recognized()
        {
                string command = $"simulate 3 wakeup true";
                Assert.IsTrue(SimulateHelper.MatchesSimulate(command));

                var helper = new SimulateHelper(command, dummyController);
                helper.CreateCommand();
                CheckWakeupCommand(helper.Command);
        }

        [TestMethod]
        public void SimulateRTR()
        {
            var helper = new SimulateHelper("simulate 3 rtr true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT), helper.Command.GetType());
            var report = (COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT)helper.Command;
            Assert.AreEqual((byte)1, report.properties1.operatingState);

            helper = new SimulateHelper("simulate 3 rtr false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT), helper.Command.GetType());
            report = (COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT)helper.Command;
            Assert.AreEqual((byte)0, report.properties1.operatingState);
        }

        private void CheckBatteryCommand(object command, byte value)
        {
            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BATTERY.BATTERY_REPORT), command.GetType());
            var cmd = (COMMAND_CLASS_BATTERY.BATTERY_REPORT)command;
            Assert.AreEqual(value, cmd.batteryLevel);
        }

        private void CheckWakeupCommand(object command)
        {
            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION), command.GetType());
        }

        private static byte[] BatteryValues()
        {
            return new byte[] { 0, 1, 10, 20, 50, 99, 100, 255 };
        }

        private void CheckCentralSceneCommand(object command, int sceneNumber)
        {
            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_NOTIFICATION), command.GetType());
            var cmd = (COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_NOTIFICATION)command;

            Assert.AreEqual(sceneNumber, cmd.sceneNumber, $"sceneId={sceneNumber}");
            Assert.AreEqual(1, cmd.properties1.slowRefresh, $"sceneId={sceneNumber}"); //like NanoMote
        }

        private static void CheckMultiChannelCommand(object command, byte channel, bool value)
        {
            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP), command.GetType());
            var cmd = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)command;
            Assert.AreEqual(channel, cmd.properties1.sourceEndPoint);
            Assert.AreEqual(channel, cmd.properties2.destinationEndPoint);
            Assert.AreEqual(COMMAND_CLASS_BASIC_V2.ID, cmd.commandClass);
            Assert.AreEqual(COMMAND_CLASS_BASIC_V2.BASIC_REPORT.ID, cmd.command);
            Assert.AreEqual(1, cmd.parameter.Count);
            int expectedParameter = value ? 255 : 0;
            Assert.AreEqual(expectedParameter, cmd.parameter[0]);
        }

        [TestMethod]
        public void SimulateOn()
        {
            var helper = new SimulateHelper("simulate true", dummyController);
            Assert.AreEqual(true, helper.GetSimulationMode());
        }

        [TestMethod]
        public void SimulateOff()
        {
            var helper = new SimulateHelper("simulate false", dummyController);
            Assert.AreEqual(false, helper.GetSimulationMode());
        }

        [TestMethod]
        public void SimulateOff_NoController_StaysOn()
        {
            var helper = new SimulateHelper("simulate false", null);
            Assert.AreEqual(true, helper.GetSimulationMode());
        }
    }
}
