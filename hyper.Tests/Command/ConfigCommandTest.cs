using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace hyper.Tests.Command
{
    [TestClass]
    public class ConfigCommandTest
    {
        [TestMethod]
        public void IsMatch_WithoutProfile()
        {
            bool isMatch = ConfigCommand.IsMatch("config 2");

            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetNodeIdTest()
        {
            const byte NodeId = 2;
            string command = "config " + NodeId;

            byte actualId = ConfigCommand.GetNodeId(command);

            Assert.AreEqual(NodeId, actualId);
        }

        [TestMethod]
        public void IsRetry_WithExclamationMark_ReturnsTrue()
        {
            bool retry = ConfigCommand.IsRetry("config 2!");
            Assert.IsTrue(retry);
        }

        [TestMethod]
        public void IsRetry_WithoutExclamationMark_ReturnsFalse()
        {
            bool retry = ConfigCommand.IsRetry("config 2");
            Assert.IsFalse(retry);
        }

        [TestMethod]
        public void GetProfile_ProfileSet_ReturnsProfile()
        {
            const string ExpectedProfile = "battery";
            const string Command = "config 2 " + ExpectedProfile;

            bool isMatch = ConfigCommand.IsMatch(Command);
            string profile = ConfigCommand.GetProfile(Command);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
        }

        [TestMethod]
        public void GetProfileAndRetry_ProfileAndRetrySet_ReturnsProfile()
        {
            const string ExpectedProfile = "battery";
            const string Command = "config 2 " + ExpectedProfile + "!";

            bool isMatch = ConfigCommand.IsMatch(Command);
            string profile = ConfigCommand.GetProfile(Command);
            bool retry = ConfigCommand.IsRetry(Command);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
            Assert.IsTrue(retry);
        }
    }
}
