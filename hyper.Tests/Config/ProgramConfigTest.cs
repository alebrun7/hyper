using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using hyper.config;

namespace hyper.Tests.Config
{
    [TestClass]
    public class ProgramConfigTest
    {
        string configExample = "numRetriesForBasic: 0\r\nretryDelayForBasic: 10\r\nretryDelaysForBasic: 5 10 20 40 60 60 60 60 60 60 60 60 60 60\r\n";

        [TestMethod]
        public void ParseTest()
        {
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
        }

        [TestMethod]
        public void ReadIntValueTest()
        {
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
            int actual = programConfig.GetIntValueOrDefault("numRetriesForBasic", 0);
            Assert.AreEqual(0, actual);
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ReadIntValue_NotParsed_ThrowsException()
        {
            var programConfig = new ProgramConfig();
            int actual = programConfig.GetIntValueOrDefault("numRetriesForBasic", 0);
        }

        [TestMethod]
        public void ReadIntValue_ValueDifferentFromDefault()
        {
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
            int actual = programConfig.GetIntValueOrDefault("numRetriesForBasic", 2);
            Assert.AreEqual(0, actual);
        }

        [TestMethod]
        public void ReadIntValue_ValueNonExisting_ReturnsDefault()
        {
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
            int actual = programConfig.GetIntValueOrDefault("notExisting", 2);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void ReadIntListValue_ReturnsList()
        {
            string defaultValue = "10";
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
            var actual = programConfig.GetIntListValueOrDefault("retryDelaysForBasic", defaultValue);
            Assert.AreEqual(14, actual.Length);
            Assert.AreEqual(5, actual[0]);
            Assert.AreEqual(60, actual[13]);
        }

        [TestMethod]
        public void ReadIntListValue_EmptyValue_ReturnsEmptyList()
        {
            string defaultValue = "";
            var programConfig = new ProgramConfig();
            programConfig.Parse(configExample);
            var actual = programConfig.GetIntListValueOrDefault("nonExisting", defaultValue);
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        public void LoadFromFileTest()
        {
            var programConfig = new ProgramConfig();
            programConfig.LoadFromFile();
            int[] actual = programConfig.GetIntListValueOrDefault("retryDelaysForBasic", "5");
            Assert.AreEqual(14, actual.Length);
        }

        [TestMethod]
        public void SerializeTest()
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            config.Add("numRetriesForBasic", "0");
            config.Add("retryDelayForBasic", "10");
            config.Add("retryDelaysForBasic", "5 10 20 40 60 60 60 60 60 60 60 60 60 60");

            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yaml = serializer.Serialize(config);

            Assert.AreEqual(configExample, yaml);
        }
    }
}
