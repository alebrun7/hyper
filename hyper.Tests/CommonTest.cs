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
        [TestMethod]
        public void GetConfigurationForDevice_TypeNotUnique_ReturnsCorrectEntry()
        {
            var device1 = "ZW139";
            var device2 = "ZW116";
            int manufacturer = 134; //for Aeotec the productId is important
            int productType = 3; //for Aeotec there are seveval devices with this produtType
            int productId1 = 139, productId2 = 116;
            var configList = new List<ConfigItem>();
            AddDevice(configList, device1, manufacturer, productType, productId1);
            AddDevice(configList, device2, manufacturer, productType, productId2);

            var config = Common.GetConfigurationForDevice(configList, manufacturer, productType, productId2);

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
        public void GetConfigurationForDevice_WrongProductIdInConfig_ReturnsConfig()
        {
            var device1 = "ZW139";
            int manufacturer = 134; //for Aeotec the productId is important
            int productType = 3; //for Aeotec there are seveval devices with this produtType
            int productId1 = 139, productId2 = 116;
            var configList = new List<ConfigItem>();
            AddDevice(configList, device1, manufacturer, productType, productId1);

            var config = Common.GetConfigurationForDevice(configList, manufacturer, productType, productId2);

            Assert.IsNull(config);
        }

        void AddDevice(List<ConfigItem> configList, string name, int manufacturerId, int productTypeId, int productId)
        {
            configList.Add(new ConfigItem
            {
                deviceName = name,
                manufacturerId = manufacturerId,
                productTypeId = productTypeId,
                productId = productId
            });
        }
    }
}
