using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests.Command
{
    [TestClass]
    public class QueueCommandTest
    {
        [TestMethod]
        public void IsMatch_WithoutProfile()
        {
            bool isMatch = QueueCommand.IsMatch("queue 2 config");
            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetNodeIds_OneId_ReturnsArray()
        {
            var actual = QueueCommand.GetNodeIds("queue 2 config");
            Assert.AreEqual(1, actual.Length);
            Assert.AreEqual(2, actual[0]);
        }

        [TestMethod]
        public void GetNodeIds_TwoIds_ReturnsArray()
        {
            var actual = QueueCommand.GetNodeIds("queue 2,3 config");
            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(2, actual[0]);
            Assert.AreEqual(3, actual[1]);
        }

        [TestMethod]
        public void GetCommandTest()
        {
            var actual = QueueCommand.GetCommand("queue 2 config");
            Assert.AreEqual("config", actual);
        }
    }
}
