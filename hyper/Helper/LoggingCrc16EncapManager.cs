using System;
using System.Collections.Generic;
using System.Text;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.CommandClasses;
using ZWave.Layers.Frame;

namespace hyper.Helper
{
    /// <summary>
    /// Wrappt LoggingCrc16EncapManager und macht dabei eine Logausgabe.
    /// Damit man im Log nachvollziehen kann, welche Command wirklich gekommen ist.
    /// </summary>
    class LoggingCrc16EncapManager : Crc16EncapManager
    {

        protected override CustomDataFrame SubstituteIncomingInternal(CustomDataFrame packet,
            byte destNodeId, byte srcNodeId, byte[] cmdData, int lenIndex,
            out ActionBase additionalAction, out ActionBase completeAction)
        {
            if (cmdData.Length > 1 && cmdData[0] == COMMAND_CLASS_CRC_16_ENCAP.ID
                && cmdData[1] == COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP.ID)
            {
                string ccName = typeof(COMMAND_CLASS_CRC_16_ENCAP).Name + ":"
                    + typeof(COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP).Name;
                Common.logger.Info($"substitute incoming {ccName} from node {srcNodeId}");
            }
            return base.SubstituteIncomingInternal(packet, destNodeId, srcNodeId, cmdData, lenIndex, out additionalAction, out completeAction);
        }
    }
}
