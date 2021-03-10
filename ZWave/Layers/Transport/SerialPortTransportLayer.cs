using System.IO.Ports;
using System.Linq;

namespace ZWave.Layers.Transport
{
    public class SerialPortTransportLayer : TransportLayer
    {
        public override ITransportListener Listener { get; set; }

        public TransportClientBase TransportClient { get; private set; }

        public override ITransportClient CreateClient(byte sessionId)
        {
            TransportClient = new SerialPortTransportClient(TransmitCallback)
            {
                SuppressDebugOutput = SuppressDebugOutput,
                SessionId = sessionId
            };
            return TransportClient;
        }

        public static string[] GetDeviceNames()
        {
            var ret = SerialPort.GetPortNames();
            if (ret != null)
            {
                ret = ret.Select(x =>
                {
                    var r = x;
                    int inx = x.IndexOf('\0');
                    if (inx > 0)
                    {
                        r = x.Substring(0, inx);
                    }
                    return r;
                }).ToArray();
            }
            return ret;
        }
    }
}
