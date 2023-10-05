using System;
using System.Collections.Generic;
using System.Text;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Enums;

namespace BasicApplication_netcore.Operations
{
    public class ReadNVRamOperation : RequestApiOperation
    {
        // the Offset is coded with 3 Bytes in CreateInputParameters so obviously it can be greater than 65536 or two bytes. We use up to 0x40000 (256K)
        private uint Offset { get; set; }
        private byte Length { get; set; }
        public ReadNVRamOperation(uint offset, byte length)
            : base(CommandTypes.CmdNVMExtRead, false)
        {
            Offset = offset;
            Length = length;
        }

        protected override byte[] CreateInputParameters()
        {
            return new byte[] { (byte)(Offset >> 16), (byte)(Offset >> 8), (byte)(Offset & 0xFF), (byte)(Length >> 8), (byte)(Length & 0xFF) };
        }

        protected override void SetStateCompleted(ActionUnit ou)
        {
            SpecificResult.RetValue = ((DataReceivedUnit)ou).DataFrame.Payload;
            base.SetStateCompleted(ou);
        }

        public ReadNVRamResult SpecificResult
        {
            get { return (ReadNVRamResult)Result; }
        }

        protected override ActionResult CreateOperationResult()
        {
            return new ReadNVRamResult();
        }
    }

    public class ReadNVRamResult : ActionResult
    {
        public byte[] RetValue { get; set; }
    }
}
