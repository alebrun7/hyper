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
        public static void SetupLogging(Target tcpTarget, Target consoleTarget)
        {
            var fileTarget = new FileTarget()
            {
                Name = "FileTarget",
                Layout = @"${longdate} ${uppercase:${level}} ${message}",
                AutoFlush = true,
                FileName = "${basedir}/logs/log.${shortdate}.txt",
                ArchiveFileName = "${basedir}/logs/archives/log.{#####}.zip",
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ConcurrentWrites = true,
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveOldFileOnStartup = true,
                EnableArchiveFileCompression = true,
                MaxArchiveFiles = 14,
                OptimizeBufferReuse = true,
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

        private static void AddTargetAndSetRules(LoggingConfiguration configuration, Target target)
        {
            configuration.AddTarget(target);
            var rules = configuration.LoggingRules.Where(rule => rule.RuleName == target.Name);
            if (rules.IsNullOrEmpty())
            {
                configuration.AddRuleForAllLevels(target);
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
