using System;
using System.Collections.Generic;

namespace hyper.Output
{
    internal class OutputManager
    {
        public static List<IOutput> Outputs = new List<IOutput>();

        public static void AddOutput(IOutput output)
        {
            Outputs.Add(output);
        }

        public static void HandleCommand(object command, byte srcNodeId, byte destNodeId)
        {
            foreach (var output in Outputs)
            {
                try
                {
                    output.HandleCommand(command, srcNodeId, destNodeId);
                }
                catch (Exception e)
                {
                    Common.logger.Error(e, "Exception caught in OutputManager.HandleCommand()");
                    Common.logger.Error($"srcNodeId={srcNodeId}, destNodeId={destNodeId}");
                    if (command == null)
                    {
                        Common.logger.Error("command is null!");
                    }
                    else
                    {
                        string commanType = command.GetType().FullName;
                        Common.logger.Error($"command is {commanType}");
                    }
                }
            }
        }

        internal static void ReadProgramConfig()
        {
            foreach (var output in Outputs)
            {
                output.ReadProgramConfig();
            }
        }
    }
}