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
        readonly string destAddress = "127.0.0.1";
        readonly int destPort = 3001;
        readonly int incomingPort = 4123;

        IPEndPoint destEndPoint;
        TransportClientBase _transportClient;
        private Socket outputSocket;
        Action<DataChunk, bool> origReceiveDataCallback;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public UDPMultiplexer(TransportClientBase transportClient)
        {
            _transportClient = transportClient;
        }

        public void Start()
        {
            IPAddress ipAddress = IPAddress.Parse(destAddress);
            destEndPoint = new IPEndPoint(ipAddress, destPort);
            outputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            logger.Info($"UdpClient will write to {destAddress}:{destPort}");

            Listen();

            //Data from Serial to udp client
            origReceiveDataCallback = _transportClient.ReceiveDataCallback;
            _transportClient.ReceiveDataCallback = HandleData;

        }

        void HandleData(DataChunk dataChunk, bool isFromFile)
        {
            logger.Debug($"UdpMultiplexer sends {dataChunk.DataBufferLength} bytes");
            origReceiveDataCallback(dataChunk, isFromFile);
            outputSocket.SendTo(dataChunk.GetDataBuffer(), destEndPoint);
        }

        void Listen()
        {
            var inputPort = new UdpClient(incomingPort, AddressFamily.InterNetwork);
            logger.Info($"UdpClient created, listening on port {incomingPort}");

            try
            {
                logger.Info("UDPMultiplexer: start listening...");
                Task udplistenTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        UdpReceiveResult received = await inputPort.ReceiveAsync();
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
