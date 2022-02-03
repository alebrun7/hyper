using hyper.commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Command
{
    public abstract class BaseCommand : ICommand
    {
        internal const string OneTo255Regex = @"\b(?:[1-9]|[1-8][0-9]|9[0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\b";
        internal const string ZeroTo255Regex = @"\b(?:[0-9]|[1-8][0-9]|9[0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\b";

        public byte NodeId { get; set; } = 0;

        public bool Retry { get; set; } = false;

        public abstract bool Start();

        public abstract void Stop();
    }
}