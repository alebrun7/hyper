using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using System.Linq;
using System.Reflection;

namespace hyper.Tests
{
    [TestClass]
    public class LoggingSetupHelperTest
    {
        [TestMethod]
        public void SetupLogging_ExistingRules_TargetsCreatedAndAssignedToRules()
        {
            //Arrange
            var config = GetTestConfig();
            //just check the test setup
            Assert.AreNotEqual(null, config);
            Assert.AreEqual(3, config.LoggingRules.Count);
            LogManager.Configuration = config;
            Target consoleFakeTarget = new NullTarget("ConsoleInput");
            Target tcpInputFakeTarget = new NullTarget("TCPInput");

            //Act
            LoggingSetupHelper.SetupLogging(tcpInputFakeTarget, consoleFakeTarget);

            //Assert
            var consoleInputRule = LogManager.Configuration.FindRuleByName("ConsoleInput");
            Assert.AreEqual(1, consoleInputRule.Targets.Count, "ConsoleInput rule should have one target");
            Assert.IsTrue(consoleInputRule.Targets.Contains(consoleFakeTarget),
                "ConsoleInput rule should contain ConsoleInput target");

            var tcpInputRule = LogManager.Configuration.FindRuleByName("TCPInput");
            Assert.AreEqual(1, tcpInputRule.Targets.Count, "TCPInput rule should have one target");
            Assert.IsTrue(tcpInputRule.Targets.Contains(tcpInputFakeTarget),
                "TCPInput rule should contain TCPInput target");

            var fileTargetRule = LogManager.Configuration.FindRuleByName("FileTarget");
            Assert.AreEqual(1, fileTargetRule.Targets.Count, "FileTarget rule should have one target");
            Assert.AreEqual("FileTarget", fileTargetRule.Targets[0].Name,
                "FileTarget rule should contain FileTarget target");
        }

        [TestMethod]
        public void SetupLogging_MissingRules_TargetsAndDefaultRulesCreated()
        {
            //Arrange
            LogManager.Configuration = new LoggingConfiguration();
            Target consoleFakeTarget = new NullTarget("ConsoleInput");
            Target tcpInputFakeTarget = new NullTarget("TCPInput");

            //Act
            LoggingSetupHelper.SetupLogging(tcpInputFakeTarget, consoleFakeTarget);

            //Assert
            var config = LogManager.Configuration;
            Assert.AreNotEqual(null, config);
            Assert.AreEqual(3, config.LoggingRules.Count);

            var consoleRules = config.LoggingRules.Where(r => r.Targets.Contains(consoleFakeTarget));
            Assert.AreEqual(1, consoleRules.Count());
            Assert.IsTrue(consoleRules.First().Levels.Contains(LogLevel.Info));
            Assert.IsFalse(consoleRules.First().Levels.Contains(LogLevel.Debug));

            var tcpRules = config.LoggingRules.Where(r => r.Targets.Contains(tcpInputFakeTarget));
            Assert.AreEqual(1, tcpRules.Count());
            Assert.IsTrue(tcpRules.First().Levels.Contains(LogLevel.Info));
            Assert.IsFalse(tcpRules.First().Levels.Contains(LogLevel.Debug));

            var fileRules = config.LoggingRules.Where(r => r.Targets.First() != null && r.Targets.First().Name == "FileTarget");
            Assert.AreEqual(1, fileRules.Count());
            Assert.IsTrue(fileRules.First().Levels.Contains(LogLevel.Info));
            Assert.IsFalse(fileRules.First().Levels.Contains(LogLevel.Debug));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            LogManager.Configuration = new LoggingConfiguration(); //cleanup
        }

        LoggingConfiguration GetTestConfig()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("test.nlog.config"));
            using (Stream xmlStream = assembly.GetManifestResourceStream(resourcePath))
            using (var xmlReader = System.Xml.XmlReader.Create(xmlStream))
            {
                return new NLog.Config.XmlLoggingConfiguration(xmlReader, null);
            }
        }
    }
}
