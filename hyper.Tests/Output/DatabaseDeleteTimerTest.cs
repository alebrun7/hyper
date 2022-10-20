using hyper.Output;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests.Output
{
    [TestClass]
    public class DatabaseDeleteTimerTest
    {

        [TestMethod]
        public void GetDueTimeTest_Sameday()
        {
            var now = DateTime.Parse("2022-10-18 9:07");
            var actual = DatabaseDeleteTimer.GetDueTime(14, 10, now);
            var expected = new TimeSpan(5, 3, 0);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetDueTimeTest_NextDAy()
        {
            var now = DateTime.Parse("2022-10-18 9:07");
            var actual = DatabaseDeleteTimer.GetDueTime(2, 1, now);
            var expected = new TimeSpan(16, 54, 0);
            Assert.AreEqual(expected, actual);
        }
    }
}