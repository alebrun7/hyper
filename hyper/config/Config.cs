﻿using System.Collections.Generic;

namespace hyper.config
{
    public class ConfigItemList
    {
        public List<ConfigItem> configItems;
    }

    public class ConfigItem
    {
        public string deviceName;
        public int manufacturerId;
        public int productTypeId;
        public int productId;
        public string profile;
        public Dictionary<byte, byte> groups = new Dictionary<byte, byte>();
        public Dictionary<string, int> config = new Dictionary<string, int>();
        public int wakeup;
    }

    public class GroupConfig
    {
        public int identifier;
        public int member;
    }

    public class ParameterConfig
    {
        public int parameter;
        public int value;
    }
}