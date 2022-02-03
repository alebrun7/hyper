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
            bool isMatch = IncludeCommand.IsMatch("include");

            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetRegex_DoNotMachIncluded()
        {
            bool isMatch = IncludeCommand.IsMatch("included");

            Assert.IsFalse(isMatch);
        }

        [TestMethod]
        public void GetRegex_WithProfile()
        {
            const string ExpectedProfile = "battery";
            const string Command = "include " + ExpectedProfile;

            bool isMatch = IncludeCommand.IsMatch(Command);
            string profile = IncludeCommand.GetProfile(Command);

            Assert.IsTrue(isMatch);
            Assert.AreEqual(ExpectedProfile, profile);
        }
    }
}
