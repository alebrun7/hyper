using hyper.Helper;
using hyper.Output;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests.Output
{
    [TestClass]
    public class BinaryFilterForMS7Test
    {
        byte srcNodeId = 7;
        Dictionary<byte, (DateTime, float)> eventMap;
        BinaryFilterForMS7 filter;

        [TestInitialize]
        public void Initialize()
        {
            eventMap = new Dictionary<byte, (DateTime, float)>();
            filter = new hyper.Output.BinaryFilterForMS7(eventMap);
        }


        [TestMethod]
        public void eventMapEmpty_StoresEventAndReturnsFalse()
        {
            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BINARY, 1f);

            Assert.IsFalse(actual);
            Assert.IsTrue(eventMap.ContainsKey(srcNodeId));
        }

        [TestMethod]
        public void shouldIgnore_eventMapEmptyBasicEvent_ReturnsFalse()
        {
            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BASIC, 1f);

            Assert.IsFalse(actual);
            Assert.IsFalse(eventMap.ContainsKey(srcNodeId));
        }

        [TestMethod]
        public void ShouldIgnore_BasicAfterBinary_ReturnsFalse()
        {
            DateTime lastTime = DateTime.Now;
            lastTime.AddSeconds(-50);
            eventMap[srcNodeId] = (DateTime.Now.AddSeconds(-50), 1f);

            bool shouldIgnore = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BASIC, 1f);
            var (tempTime, tempValue) = eventMap[srcNodeId];
            double ageInSec = (DateTime.Now - tempTime).TotalSeconds;

            Assert.IsFalse(shouldIgnore);
            Assert.IsTrue(ageInSec >= 0);
            Assert.IsTrue(ageInSec > 30); //stored event is not new
        }

        [TestMethod]
        public void ShouldIgnore_RepeatedOne_ReturnsTrue()
        {
            DateTime lastTime = DateTime.Now;
            lastTime.AddSeconds(-50);
            eventMap[srcNodeId] = (DateTime.Now.AddSeconds(-50), 1f);

            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BINARY, 1f);
            var (tempTime, tempValue) = eventMap[srcNodeId];
            double ageInSec = (DateTime.Now - tempTime).TotalSeconds;

            Assert.IsTrue(actual);
            Assert.IsTrue(ageInSec >= 0);
            Assert.IsTrue(ageInSec < 30); //stored event is new
        }

        [TestMethod]
        public void ShouldIgnore_OneAfterZero_ReturnsFalse()
        {
            DateTime lastTime = DateTime.Now;
            lastTime.AddSeconds(-50);
            eventMap[srcNodeId] = (DateTime.Now.AddSeconds(-50), 0f);

            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BINARY, 1f);
            var (tempTime, tempValue) = eventMap[srcNodeId];
            double ageInSec = (DateTime.Now - tempTime).TotalSeconds;

            Assert.IsFalse(actual);
            Assert.IsTrue(ageInSec >= 0);
            Assert.IsTrue(ageInSec < 30); //stored event is new
        }

        [TestMethod]
        public void ShouldIgnore_ZeroAfterOne_ReturnsFalse()
        {
            DateTime lastTime = DateTime.Now;
            lastTime.AddSeconds(-50);
            eventMap[srcNodeId] = (DateTime.Now.AddSeconds(-50), 1f);

            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BINARY, 0f);
            var (tempTime, tempValue) = eventMap[srcNodeId];
            double ageInSec = (DateTime.Now - tempTime).TotalSeconds;

            Assert.IsFalse(actual);
            Assert.IsTrue(ageInSec >= 0);
            Assert.IsTrue(ageInSec < 30); //stored event is new
        }

        [TestMethod]
        public void ShouldIgnore_OneAfterOneMinute_ReturnsFalse()
        {
            DateTime lastTime = DateTime.Now;
            lastTime.AddSeconds(-50);
            eventMap[srcNodeId] = (DateTime.Now.AddSeconds(-60), 1f);

            bool actual = filter.ShouldIgnore(srcNodeId, Enums.EventKey.BINARY, 1f);
            var (tempTime, tempValue) = eventMap[srcNodeId];
            double ageInSec = (DateTime.Now - tempTime).TotalSeconds;

            Assert.IsFalse(actual);
            Assert.IsTrue(ageInSec >= 0);
            Assert.IsTrue(ageInSec < 30); //stored event is new
        }
    }
}
