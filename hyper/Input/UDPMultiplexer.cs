using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWave.Layers;

namespace hyper.Input
{
    internal class UDPMultiplexer
    {
        readonly string address = "127.0.0.1";
        readonly int destPort = 3001;
        readonly int incomingPort = 4123;
        TransportClientBase _transportClient;
        UdpClient _port;
        Action<DataChunk, bool> origReceiveDataCallback;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public UDPMultiplexer(TransportClientBase transportClient)
        {
            _transportClient = transportClient;
        }
        public void Start()
        {
            _port = new UdpClient(incomingPort, AddressFamily.InterNetwork);
            logger.Info($"UdpClient created, listening on port {incomingPort}");
            _port.Connect(address, destPort);
            logger.Info($"UdpClient will write to {address}:{destPort}");

            Listen();

            //Data from Serial to udp client
            origReceiveDataCallback = _transportClient.ReceiveDataCallback;
            _transportClient.ReceiveDataCallback = HandleData;

        }

        void HandleData(DataChunk dataChunk, bool isFromFile)
        {
            logger.Debug($"UdpMultiplexer sends {dataChunk.DataBufferLength} bytes");
            origReceiveDataCallback(dataChunk, isFromFile);
            _port.Send(dataChunk.GetDataBuffer(), dataChunk.DataBufferLength);
        }

        void Listen()
        {
            try
            {
                logger.Debug("UDPMultiplexer: extra debug output...");
                logger.Info("UDPMultiplexer: start listening...");
                Task udplistenTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        UdpReceiveResult received = await _port.ReceiveAsync();
                        byte[] data = received.Buffer;
                        logger.Debug($"UdpMultiplexer received {data.Length} bytes");
                        _transportClient.WriteData(data);
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error(this.GetType().Name + ": " + e.Message);
            }
        }
    }

}
