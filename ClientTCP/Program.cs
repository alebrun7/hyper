using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ClientTCP
{
    class Program
    {
        static string[] GetWaitStrings(string command)
        {
            const string waitForPrefix = "wait for ";
            if (command.StartsWith(waitForPrefix)) {
                //Allow to wait for a specific string during automatic testing
                return new string[] { command.Substring(waitForPrefix.Length) };
            }
            else if (command.StartsWith("replace"))
            {
                // replace can fehlschlagen, daher zwei verschiedene string zu prüfen
                return new string[] {
                    "Set new device to inclusion mode",
                    "If node is reachable, we cannot replace it"
                };
            }
            else switch (command)
            {
                case "include":
                    return new string[] { "Starting inclusion, please wake up device..." };

                default:
                    return new string[] { "destiny" };
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
                client.WaitStrings = GetWaitStrings(cmd);
                if (cmd.StartsWith("wait"))
                {
                    Console.WriteLine("Waiting for: " + client.WaitStrings[0]);
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
