﻿using hyper.Input;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace hyper.Inputs
{
    [Target("TCPInput")]
    public sealed class TCPInput : TargetWithLayout, IInput
    {
        //  public bool CanRead { get; set; } = false;

        //  private string currentMessage = "";
        private readonly object _syncObj = new object();

        public event ConsoleCancelEventHandler CancelKeyPress;

        private TCPServer server;
        //    private ManualResetEvent resetEvent;

        private BlockingCollection<string> messageQueue = new BlockingCollection<string>();

        public TCPInput(int port)
        {
            Name = "TCPInput";
            if (port > 0) //for tests
            {
                server = new TCPServer(IPAddress.Any, port);
                server.OnMessage += OnMessage;
                server.Start();
            }
        }

        //private void OnConnected(TcpSession client)
        //{
        //    Console.WriteLine("added client!");
        //    clients.Add(client);
        //}

        public void OnMessage(string wholeMessage)
        {
            if (wholeMessage?.Trim().Length > 0)
            {
                var messageList = wholeMessage.Trim().Split("\n");
                foreach (string message in messageList) {
                    if (message == "stop")
                    {
                        CancelKeyPress?.Invoke(null, null);
                        return;
                    }
                    if (message == "cancel")
                    {
                        //handled in CancelHandler.CancelHandler()
                        CancelKeyPress?.Invoke(this, null);
                    }
                    lock (_syncObj)
                        messageQueue.Add(message);
                }
            }
        }

        //public bool Available()
        //{
        //    lock (_syncObj)
        //        return messageQueue.Count > 0;
        //}

        //public string Read()
        //{
        //    lock (_syncObj)
        //        return messageQueue.Dequeue();
        //}

        //public void Flush()
        //{
        //    currentMessage = "";
        //}

        protected override void Write(LogEventInfo logEvent)
        {
            string logMessage = this.Layout.Render(logEvent);

            server.Multicast(logMessage + Environment.NewLine);

            //Console.WriteLine("wrrite to clieont: {0}", logMessage);

            //foreach (var client in clients)
            //{
            //client.Send(logMessage);
            ////    server.streamWriter.WriteLineAsync(logMessage);

            //}
        }

        //public void SetResetEvent(ManualResetEvent resetEvent)
        //{
        //    this.resetEvent = resetEvent;
        //}

        public void Interrupt()
        {
            server.Stop();
        }

        public void SetQueue(BlockingCollection<string> ownQueue)
        {
            messageQueue = ownQueue;
        }
    }
}