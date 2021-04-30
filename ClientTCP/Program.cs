using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ClientTCP
{
    class Program
    {

        static void Main(string[] args)
        {
            const int port = 5432;
            string address = "127.0.0.1";
            if (args.Length > 0)
                address = args[0];

            var client = new TCPClient(address, port);

            // Connect the client
            Console.WriteLine("Client connecting to {0}:{1}...", address, port);
            client.ConnectAsync();
            Console.WriteLine("Done!");

            while (!client.IsConnected)
            {
                System.Threading.Thread.Sleep(100);
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
