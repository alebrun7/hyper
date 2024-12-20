﻿using NLog;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hyper.Inputs
{
    [Target("ConsoleInput")]
    public sealed class ConsoleInput : TargetWithLayout, IInput
    {
        // public bool CanRead { get; set; } = false;

        // private string currentMessage = "";
        private readonly object _syncObj = new object();

        private BlockingCollection<string> messageQueue = new BlockingCollection<string>();

        private Task backgroundTask = null;
        //  private ManualResetEvent resetEvent;

        public event ConsoleCancelEventHandler CancelKeyPress;

        public ConsoleInput()
        {
            Name = "ConsoleInput";
            
        }

        public void StartReadingInput()
        {
            backgroundTask = new Task(() =>
            {
                while (true)
                {
                    var message = Console.ReadLine();
                    if (message?.Trim().Length > 0)
                    {
                        if (message == "stop")
                        {
                            CancelKeyPress?.Invoke(null, null);
                            continue;
                        }
                        lock (_syncObj)
                            messageQueue.Add(message);
                        //       resetEvent.Set();
                    }
                    if (message == null)
                    {
                        //can happen if run as a service without screen
                        //or once if Ctrl+C is pressed to stop a command
                        //in the first case we could break to close the thread
                        //but in the second case we want to accept more commands so stay in the loop
                        //it could use too much cpu so make a pause
                        Thread.Sleep(500);
                    }
                    //if (CanRead)
                    //{
                    //    currentMessage = message;
                    //}
                    //else
                    //{
                    //    if (message == "stop")
                    //    {
                    //        CancelKeyPress?.Invoke(null, null);
                    //    }
                    //}
                }
            });
            backgroundTask.Start();
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

            Console.WriteLine(logMessage);
        }

        //public void SetResetEvent(ManualResetEvent resetEvent)
        //{
        //    this.resetEvent = resetEvent;
        //}

        public void Interrupt()
        {
            //TODO
            //stop console read
        }

        public void SetQueue(BlockingCollection<string> ownQueue)
        {
            this.messageQueue = ownQueue;
        }
    }
}