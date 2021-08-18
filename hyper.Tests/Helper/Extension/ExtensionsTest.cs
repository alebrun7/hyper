using hyper.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.CommandClasses;
using static hyper.Helper.Extension.Extensions;

namespace hyper.Tests.Helper.Extension
{
    [TestClass]
    public class ExtensionsTest
    {
        Enums.EventKey eventType;
        float floatVal;

        [TestCleanup]
        public void Cleanup()
        {
            eventType = 0;
            floatVal = 0;
        }

        [TestMethod]
        public void NotificationType_knowType()
        {
            var t = (NotificationType)0x07;
            Assert.AreEqual(NotificationType.HomeSecurity, t);
        }
        [TestMethod]
        public void NotificationType_unknowType_doNotThrow()
        {
            byte unknownValue = 0xF3;
            var cmd = NewNotificationReport(unknownValue, unknownValue); //NotificationType.AccessControl

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);


            Assert.IsFalse(recognized);
            Assert.AreEqual(Enums.EventKey.UNKNOWN, eventType);
            Assert.AreEqual(-1, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_WindowDoorOpen()
        {
            //typical event for MK
            // 0x06:  NotificationType.AccessControl;
            var cmd = NewNotificationReport(0x06, 0x16); //NotificationType.AccessControl, AccessControlEvent.WindowDoorIsOpen;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.STATE_CLOSED, eventType);
            Assert.AreEqual(0, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_WindowDoorClosed()
        {
            //typical event for MK
            var cmd = NewNotificationReport(0x06, 0x17); //NotificationType.AccessControl, AccessControlEvent.WindowDoorIsClosed;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.STATE_CLOSED, eventType);
            Assert.AreEqual(1, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_MotionDectection()
        {
            //typical event for BW
            var cmd = NewNotificationReport(0x07, 0x08); //NotificationType.HomeSecurity, HomeSecurityEvent.MotionDetection;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.MOTION, eventType);
            Assert.AreEqual(1, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_MotionFinished()
        {
            //typical event for BW
            var cmd = NewNotificationReport(0x07, 0x00); //NotificationType.HomeSecurity, HomeSecurityEvent.StateIdle;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.MOTION, eventType);
            Assert.AreEqual(0, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_RelaisOn()
        {
            //
            var cmd = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT();
            cmd.currentValue = 255;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.STATE_ON, eventType);
            Assert.AreEqual(1, floatVal);
        }

        [TestMethod]
        public void GetKeyValue_RelaisOff()
        {
            //
            var cmd = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT();
            cmd.currentValue = 0;

            bool recognized = cmd.GetKeyValue(out eventType, out floatVal);

            Assert.IsTrue(recognized);
            Assert.AreEqual(Enums.EventKey.STATE_ON, eventType);
            Assert.AreEqual(0, floatVal);
        }

        private static COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT NewNotificationReport(byte type, byte mevent)
        {
            var cmd = new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT();
            cmd.notificationType = type;
            cmd.mevent = mevent;
            return cmd;
        }
    }
}
