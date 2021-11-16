using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using ZWave.CommandClasses;

namespace hyper.Tests.Command
{
    [TestClass]
    public class SimulateHelperTest
    {
        Regex regex = SimulateHelper.GetSimulateRegex(InteractiveCommand.OneTo255Regex);
        Regex regexOnOff = SimulateHelper.GetSimulateOnOffRegex(InteractiveCommand.OneTo255Regex);
        object dummyController = new object();

        [TestMethod]
        public void GetSimulateRegexTest()
        {
            Regex regex = SimulateHelper.GetSimulateRegex(InteractiveCommand.OneTo255Regex);
            for (int nodeId = 0; nodeId <= 1000; ++nodeId)
            {
                bool expected = 0 < nodeId && nodeId < 256;
                string cmd = $"simulate {nodeId} bw true";
                Assert.AreEqual(expected, regex.IsMatch(cmd), $"IsMatch should be {expected} for cmd={cmd}");
            }
        }

        [TestMethod]
        public void SimulateBin()
        {
            var helper = new SimulateHelper(regex, "simulate 3 bin true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            var basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(255, basic_set.value);
            Assert.AreEqual(3, helper.NodeId);

            helper = new SimulateHelper(regex, "simulate 3 bin false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(0, basic_set.value);
        }

        [TestMethod]
        public void SimulateBw()
        {
            var helper = new SimulateHelper(regex, "simulate 3 bw true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT), helper.Command.GetType());
            var report = (COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT)helper.Command;
            Assert.AreEqual(255, report.sensorValue);

            helper = new SimulateHelper(regex, "simulate 3 bw false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT), helper.Command.GetType());
            report = (COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT)helper.Command;
            Assert.AreEqual(0, report.sensorValue);
        }

        [TestMethod]
        public void SimulateFt()
        {
            var helper = new SimulateHelper(regex, "simulate 3 ft true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            var basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(255, basic_set.value);
            Assert.AreEqual(3, helper.NodeId);

            helper = new SimulateHelper(regex, "simulate 3 ft false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_BASIC_V2.BASIC_SET), helper.Command.GetType());
            basic_set = (COMMAND_CLASS_BASIC_V2.BASIC_SET)helper.Command;
            Assert.AreEqual(0, basic_set.value);
        }

        [TestMethod]
        public void SimulateMk()
        {
            var helper = new SimulateHelper(regex, "simulate 3 mk true", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT), helper.Command.GetType());
            var report = (COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT)helper.Command;
            Assert.AreEqual((byte)NotificationType.AccessControl, report.notificationType);
            Assert.AreEqual((byte)AccessControlEvent.WindowDoorIsOpen, report.mevent);

            helper = new SimulateHelper(regex, "simulate 3 mk false", dummyController);
            helper.CreateCommand();
            Assert.IsNotNull(helper.Command);
            Assert.AreEqual(typeof(COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT), helper.Command.GetType());
            report = (COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT)helper.Command;
            Assert.AreEqual((byte)NotificationType.AccessControl, report.notificationType);
            Assert.AreEqual((byte)AccessControlEvent.WindowDoorIsClosed, report.mevent);
        }

        [TestMethod]
        public void SimulateOn()
        {
            var helper = new SimulateHelper(regexOnOff, "simulate true", dummyController);
            Assert.AreEqual(true, helper.GetSimulationMode());
        }

        [TestMethod]
        public void SimulateOff()
        {
            var helper = new SimulateHelper(regexOnOff, "simulate false", dummyController);
            Assert.AreEqual(false, helper.GetSimulationMode());
        }

        [TestMethod]
        public void SimulateOff_NoController_StaysOn()
        {
            var helper = new SimulateHelper(regexOnOff, "simulate false", null);
            Assert.AreEqual(true, helper.GetSimulationMode());
        }
    }
}
