using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace hyper.config
{
    public class ProgramConfig
    {
        Dictionary<string, string> config;
        const string CONFIG_FILE = "programconfig.yaml";
        const string CONFIG_FILE_TEMPLATE = "programconfig_template.yaml";

        public void LoadFromFile()
        {
            if (File.Exists(CONFIG_FILE))
            {
                var yamlText = File.ReadAllText(CONFIG_FILE);
                Parse(yamlText);
            }
            else if (File.Exists(CONFIG_FILE_TEMPLATE))
            {
                var yamlText = File.ReadAllText(CONFIG_FILE_TEMPLATE);
                Parse(yamlText);

            }
            else
            {
                Common.logger.Warn($"config files {CONFIG_FILE} and {CONFIG_FILE_TEMPLATE} not found");
                config = new Dictionary<string, string>();
            }
        }

        //extracted from LoadFromFile() for easier testing. not to be used in production.
        public void Parse(string configExample)
        {
            var deserializer = new DeserializerBuilder()
               .WithNamingConvention(CamelCaseNamingConvention.Instance)
               .Build();
            config = deserializer.Deserialize<Dictionary<string, string>>(configExample);
        }

        public int GetIntValueOrDefault(string key, int defaultValue)
        {
            ValidateParsedState();
            return int.Parse(config.GetValueOrDefault(key, defaultValue.ToString()));
        }

        public int[] GetIntListValueOrDefault(string key, string defaultValue)
        {
            ValidateParsedState();
            var val = config.GetValueOrDefault(key, defaultValue.ToString());
            if (!string.IsNullOrEmpty(val))
            {
                var stringValues = val.Split();
                var ret = new int[stringValues.Length];
                for (int i = 0; i < stringValues.Length; ++i)
                {
                    ret[i] = int.Parse(stringValues[i]);
                }
                return ret;
            }
            else
            {
                return new int[0];
            }
        }

        public string GetStringValueOrDefault(string key, string defaultValue)
        {
            ValidateParsedState();
            var val = config.GetValueOrDefault(key, defaultValue);
            return val;
        }

        private void ValidateParsedState()
        {
            if (config == null)
            {
                throw new InvalidOperationException("call Parse() or Load() first");
            }
        }
    }
}
