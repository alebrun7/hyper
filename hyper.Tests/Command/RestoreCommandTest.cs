using hyper.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests.Command
{
    [TestClass]
    public class RestoreCommandTest
    {

        [TestMethod]
        public void DateTest()
        {
            var dt = new DateTime(2022, 06, 29, 14, 35, 02);
            string dateStr = dt.ToString("yyyy-MM-dd-HH-mm");
            Assert.AreEqual("2022-06-29-14-35", dateStr);
        }

        [TestMethod]
        public void IsMatch_WithoutFileName_ReturnsTrue()
        {
            bool actual = RestoreCommand.IsMatch("restore!");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void IsMatch_WithFileName_ReturnsTrue()
        {
            bool actual = RestoreCommand.IsMatch("restore eeprom_f37d4eff_11_35.bin!");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void IsMatch_WithFileNameAndHypen_ReturnsTrue()
        {
            bool actual = RestoreCommand.IsMatch("restore eeprom.bin-DVRT-OG-02!");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void GetFileName_WithoutFileName_ReturnsDefault()
        {
            string actual = RestoreCommand.GetFileName("restore!");
            Assert.AreEqual("eeprom.bin", actual);
        }

        [TestMethod]
        public void GetFileName_WithFileName_ReturnsFileNam()
        {
            const string c_fileName = "f37d4eff_11_35.bin";
            string actual = RestoreCommand.GetFileName($"restore {c_fileName}!");
            Assert.AreEqual(c_fileName, actual);
        }
    }
}
