using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace hyper.Helper
{
    public class LoggingSetupHelper
    {
        public static string Layout
        {
            get
            {
                return "${longdate} ${uppercase:${level}} ${message} ${exception:format=toString}";
            }
        }

        public static void SetupLogging(Target tcpTarget, Target consoleTarget)
        {
            var fileTarget = new FileTarget()
            {
                Name = "FileTarget",
                Layout = Layout,
                AutoFlush = true,
                FileName = "${basedir}/logs/log.${shortdate}.txt",
                ArchiveFileName = "${basedir}/logs/archives/log.{#####}.zip",
                ArchiveSuffixFormat = "_{1:yyyMMdd}_{0:00}",
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 14,
                CreateDirs = true
            };

            var configuration = LogManager.Configuration;
            if (configuration == null)
            {
                configuration = new LoggingConfiguration();
            }
            AddTargetAndSetRules(configuration, fileTarget);
            AddTargetAndSetRules(configuration, consoleTarget);
            AddTargetAndSetRules(configuration, tcpTarget);
            LogManager.Configuration = configuration;
        }

        public static void SetDebugLevel(bool enabled)
        {
            var configuration = LogManager.Configuration;
            if (configuration != null)
            {
                foreach (var r in configuration?.LoggingRules)
                {
                    if (enabled)
                    {
                        r.EnableLoggingForLevel(LogLevel.Debug);
                    }
                    else
                    {
                        r.DisableLoggingForLevel(LogLevel.Debug);
                    }
                }
                LogManager.Configuration = configuration;
            }
        }

        private static void AddTargetAndSetRules(LoggingConfiguration configuration, Target target)
        {
            configuration.AddTarget(target);
            var rules = configuration.LoggingRules.Where(rule => rule.RuleName == target.Name);
            if (rules.IsNullOrEmpty())
            {
                configuration.AddRule(LogLevel.Info, LogLevel.Fatal, target);
            }
            else
            {
                foreach (var r in rules)
                {
                    r.Targets.Add(target);
                }
            }
        }
    }
}
