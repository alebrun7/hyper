using hyper.config;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Tests
{
    [TestClass]
    public class CommonTest
    {
        private const string TestProfile = "battery";
        private const string TestDeviceName = "ZW139";
        private const int TestManufacturerId = 134;
        private const int TestProductTypeId = 3;
        private const int TestProductId = 139;
        private const int TestProdutId2 = 116;
        private const string DefaultProfile = "default";

        [TestMethod]
        public void GetConfigurationForDevice_TypeNotUnique_ReturnsCorrectEntry()
        {
            var device2 = "ZW116";
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId);
            AddDevice(configList, device2, TestManufacturerId, TestProductTypeId, TestProdutId2);

            var config = Common.GetConfigurationForDevice(configList, TestManufacturerId, TestProductTypeId, TestProdutId2);

            Assert.AreEqual(device2, config.deviceName);
        }

        [TestMethod]
        public void GetConfigurationForDevice_NoProductIdInConfig_ReturnsConfig()
        {
            var deviceName = "FGDW002";
            int manufacturer = 271; //Fibaro
            int productType = 1794; //for this manufacturer the productType seems to be sufficient to identify devices
            int productId = 4096;
            var configList = new List<ConfigItem>();
            AddDevice(configList, deviceName, manufacturer, productType, 0);

            var config = Common.GetConfigurationForDevice(configList, manufacturer, productType, productId);

            Assert.IsNotNull(config);
            Assert.AreEqual(deviceName, config.deviceName);
        }

        [TestMethod]
        public void GetConfigurationForDevice_WrongProductIdInConfig_ReturnsNullConfig()
        {
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId);

            var config = Common.GetConfigurationForDevice(configList, TestManufacturerId, TestProductTypeId, TestProdutId2);

            Assert.IsNull(config);
        }

        [TestMethod]
        public void GetConfigurationForDevice_WithoutProfile_ReturnsDefault()
        {
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, TestProfile);
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, DefaultProfile);

            var config = Common.GetConfigurationForDevice(configList, TestManufacturerId, TestProductTypeId, TestProductId);

            Assert.IsNotNull(config);
            Assert.AreEqual(DefaultProfile, config.profile);
        }

        [TestMethod]
        public void GetConfigurationForDevice_WithoutProfileNullProfileExists_ReturnsNullProfile()
        {
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, TestProfile);
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId);

            var config = Common.GetConfigurationForDevice(configList, TestManufacturerId, TestProductTypeId, TestProductId);

            Assert.IsNotNull(config);
            Assert.IsNull(config.profile);
        }

        [TestMethod]
        public void GetConfigurationForDevice_WithProfile_ReturnsConfigWithProfile()
        {
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId);
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, TestProfile);

            var config = Common.GetConfigurationForDevice(configList, TestManufacturerId, TestProductTypeId, TestProductId, TestProfile);

            Assert.IsNotNull(config);
            Assert.AreEqual(TestProfile, config.profile);
        }

        [TestMethod]
        public void GetConfigurationForDevice_WithNotExistingProfile_ReturnsDefaultConfig()
        {
            var configList = new List<ConfigItem>();
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, TestProfile);
            AddDevice(configList, TestDeviceName, TestManufacturerId, TestProductTypeId, TestProductId, DefaultProfile);

            var config = Common.GetConfigurationForDevice(
                configList, TestManufacturerId, TestProductTypeId, TestProductId, "missingProfile");

            Assert.IsNotNull(config);
            Assert.AreEqual(DefaultProfile, config.profile);
        }

        [TestMethod]
        public void ParseConfig_CheckAllEntries_ProfileNotEmpty()
        {
            var configList = Common.ParseConfig("config.yaml");
            Assert.AreNotEqual(0, configList.Count);
            var configWithEmptyProfile = configList.Find(config => string.IsNullOrEmpty(config.profile));
            configList.ForEach(config =>
                Assert.IsFalse(string.IsNullOrEmpty(config.profile),"Profile is missing for device {0}", config.deviceName));
        }

        [TestMethod]
        public void ParseConfig_EachDeviceHasDefaultProfile()
        {
            var configList = Common.ParseConfig("config.yaml");
            configList.ForEach(config =>
            {
                if (!DefaultProfile.Equals(config.profile))
                {
                    var defaultConfig = Common.GetConfigurationForDevice(configList, config.manufacturerId, config.productTypeId, config.productId);
                    Assert.IsNotNull(defaultConfig, "no default profile found for device {0}", config.deviceName);
                    Assert.AreEqual(DefaultProfile, defaultConfig.profile, "Wrong default profile name for device {0}", config.deviceName);
                }
            });
        }

        void AddDevice(List<ConfigItem> configList, string name, int manufacturerId, int productTypeId, int productId, string profile = null)
        {
            configList.Add(new ConfigItem
            {
                deviceName = name,
                manufacturerId = manufacturerId,
                productTypeId = productTypeId,
                productId = productId,
                profile = profile
            }); ;
        }
    }
}
