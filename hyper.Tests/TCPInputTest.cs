using hyper.Inputs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests
{
    [TestClass]
    public class TCPInputTest
    {
        TCPInput tcpInput;

        [TestMethod]
        public void OnMessage_OneLine_OneCommand()
        {
            //Arrange
            BlockingCollection<string> messageQueue = SetupTcpInputAndQueue();

            //Act
            string cmd = "queue 1 config";
            tcpInput.OnMessage(cmd + "\n"); //not Environment.NewLine because windows/linux mix possible

            //Assert
            Assert.AreEqual(1, messageQueue.Count);
            Assert.AreEqual(cmd, messageQueue.Take());
        }

        [TestMethod]
        public void OnMessage_TwoLines_TwoCommands()
        {
            //Arrange
            BlockingCollection<string> messageQueue = SetupTcpInputAndQueue();

            //Act
            string[] cmds = new string[] { "queue 1 config", "queue 2 config" };
            string msg = String.Join("\n", cmds);
            tcpInput.OnMessage(msg + "\n");

            //Assert
            Assert.AreEqual(2, messageQueue.Count);
            Assert.AreEqual(cmds[0], messageQueue.Take());
            Assert.AreEqual(cmds[1], messageQueue.Take());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (tcpInput != null)
            {
                //Dispose() ist not implemented completely, but should be called anyway
                tcpInput.Dispose();
            }
        }

        private BlockingCollection<string> SetupTcpInputAndQueue()
        {
            const int zero_DoNotOpenAnyPortJustTesting = 0;
            tcpInput = new TCPInput(zero_DoNotOpenAnyPortJustTesting);
            BlockingCollection<string> messageQueue = new BlockingCollection<string>();
            tcpInput.SetQueue(messageQueue);
            return messageQueue;
        }
    }
}
