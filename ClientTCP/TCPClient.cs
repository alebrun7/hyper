﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace ClientTCP
{
    class TCPClient : TcpClient
    {
        public TCPClient(string address, int port) : base(address, port)
        {
            WaitStrings = new string[] { };
        }

        public string[] WaitStrings { get;  set; }

        protected override void OnConnected()
        {
            Console.WriteLine($"TCP client connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"TCP client disconnected a session with Id {Id}");

            //// Wait for a while...
            Thread.Sleep(1000);

            //// Try to connect again
            ConnectAsync();
        }


        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            var message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.Write(message);
            foreach (string waitString in WaitStrings)
            {
                if (message.Contains(waitString))
                {
                    Console.Write($"recognised wait string '{waitString}', exiting");
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"TCP client caught an error with code {error}");
        }

    }
}
