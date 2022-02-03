using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace hyper.Tests.Command
{
    [TestClass]
    public class ReplaceCommandTest
    {
        private Regex ReplaceRegex { get{ return ReplaceCommand.GetRegex(InteractiveCommand.OneTo255Regex); } }

        [TestMethod]
        public void GetRegex_WithoutProfile()
        {
            Regex regex = ReplaceCommand.GetRegex(InteractiveCommand.OneTo255Regex);

            bool isMatch = regex.IsMatch("replace 2");

            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetNodeId()
        {
            const byte NodeId = 2;
            string command = "replace " + NodeId;

            byte actualId = ReplaceCommand.GetNodeId(command, ReplaceRegex);

            Assert.AreEqual(NodeId, actualId);
        }

        [TestMethod]
        public void GetRegex_WithProfile()
        {
            Regex regex = ReplaceCommand.GetRegex(InteractiveCommand.OneTo255Regex);
            const string ExpectedProfile = "battery";
            const string Command = "replace 2 " + ExpectedProfile;

            bool isMatch = regex.IsMatch(Command);
            string profile = ReplaceCommand.GetProfile(Command, regex);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
        }
    }
}
