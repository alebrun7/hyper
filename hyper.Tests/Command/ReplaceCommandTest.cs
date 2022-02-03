using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace hyper.Tests.Command
{
    [TestClass]
    public class ReplaceCommandTest
    {
        [TestMethod]
        public void IsMatch_WithoutProfile_ReturnsTrue()
        {
            bool isMatch = ReplaceCommand.IsMatch("replace 2");

            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetNodeId()
        {
            const byte NodeId = 2;
            string command = "replace " + NodeId;

            byte actualId = ReplaceCommand.GetNodeId(command);

            Assert.AreEqual(NodeId, actualId);
        }

        [TestMethod]
        public void GetRegex_WithProfile()
        {
            const string ExpectedProfile = "battery";
            const string Command = "replace 2 " + ExpectedProfile;

            bool isMatch = ReplaceCommand.IsMatch(Command);
            string profile = ReplaceCommand.GetProfile(Command);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
        }
    }
}
