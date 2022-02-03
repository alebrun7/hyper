using hyper.Command;
using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace hyper.Tests.Command
{
    [TestClass]
    public class IncludeCommandTest
    {
        [TestMethod]
        public void GetRegex_WithoutProfile()
        {
            Regex regex = IncludeCommand.GetRegex(InteractiveCommand.OneTo255Regex);

            bool isMatch = regex.IsMatch("include");

            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetRegex_WithProfile()
        {
            Regex regex = IncludeCommand.GetRegex(InteractiveCommand.OneTo255Regex);
            const string ExpectedProfile = "battery";
            const string Command = "include " + ExpectedProfile;

            bool isMatch = regex.IsMatch(Command);
            string profile = IncludeCommand.GetProfile(Command, regex);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
        }
    }
}
