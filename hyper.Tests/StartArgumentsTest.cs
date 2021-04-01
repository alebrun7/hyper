using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using NLog.Targets;
using NLog.Config;
using NLog;

namespace hyper.Tests
{
    [TestClass]
    public class StartArgumentsTest
    {
        [TestMethod]
        public void Ctor_WithoutArgs_NotValid()
        {
            var startArgs = new StartArguments(new string[0]);
            Assert.IsFalse(startArgs.Valid);
        }

        [TestMethod]
        public void PrintUsage_WritesToLog()
        {
            MemoryTarget logTarget = LoggingSetupHelperTest.SetupMemoryLogTarget();

            new StartArguments(new string[0]).PrintUsage();

            Assert.AreNotEqual(0, logTarget.Logs.Count);
        }

        [TestMethod]
        public void Ctor_WithPort_ValidAndHasPort()
        {
            string port = "COM3";
            var startArgs = new StartArguments(new string[] { port });

            Assert.IsTrue(startArgs.Valid);
            Assert.AreEqual(port, startArgs.Port);
            Assert.IsFalse(startArgs.StartUdpMultiplexer);
        }

        [TestMethod]
        public void Ctor_WithMultiplexer_ValidAndHasMultiplexerSet()
        {
            string port = "COM3";
            var startArgs = new StartArguments(new string[] { "-udpmultiplexer", port });

            Assert.IsTrue(startArgs.Valid);
            Assert.AreEqual(port, startArgs.Port);
            Assert.IsTrue(startArgs.StartUdpMultiplexer);
        }

        [TestMethod]
        public void Ctor_WithWrongOption_InValid()
        {
            var startArgs = new StartArguments(new string[] { "-udpmultipler", "COM3" });

            Assert.IsFalse(startArgs.Valid);
        }

        [TestMethod]
        public void Ctor_OptionInWrongOrder_InValid()
        {
            var startArgs = new StartArguments(new string[] { "COM3", "-udpmultiplexer" });

            Assert.IsFalse(startArgs.Valid);
        }

        [TestMethod]
        public void Ctor_WithOneshotCommand_ValidAndHasCommand()
        {
            var startArgs = new StartArguments(new string[] { "COM3", "config", "2" });

            Assert.IsTrue(startArgs.Valid);
            Assert.AreEqual("config 2", startArgs.Command);
        }

        [TestCleanup]
        public void Cleanup()
        {
            LogManager.Configuration = new LoggingConfiguration();

        }
    }
}
