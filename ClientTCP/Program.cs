using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ClientTCP
{
    class Program
    {
        static string GetWaitString(string command)
        {
            switch(command)
            {
                case "include":
                    return "Starting inclusion, please wake up device...";
                default:
                    return "destiny";
            }
        }

        static void Main(string[] args)
        {
            const int port = 5432;
            string address = "127.0.0.1";

            if (args.Length > 0)
                address = args[0];

            string cmd = string.Empty;
            if (args.Length > 1)
            {
                cmd = String.Join(" ", args.Skip(1));
            }

            var client = new TCPClient(address, port);

            // Connect the client
            Console.WriteLine("Client connecting to {0}:{1}...", address, port);
            client.ConnectAsync();
            Console.WriteLine("Connected!");

            while (!client.IsConnected)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (!string.IsNullOrEmpty(cmd))
            {
                client.WaitString = GetWaitString(cmd);
                if (cmd.Equals("wait"))
                {
                    Console.WriteLine("Waiting for: " + client.WaitString);
                }
                else
                {
                    Console.WriteLine("Executing command: " + cmd);
                    client.SendAsync(cmd);

                }
            }

            while (true)
            {
                string line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    client.SendAsync(line + "\n");
                }
                else
                {
                    System.Threading.Thread.Sleep(500);
                    break;
                }
            }

        }




    }
}
