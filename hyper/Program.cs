using hyper.commands;
using hyper.config;
using hyper.Helper;
using hyper.Input;
using hyper.Inputs;
using hyper.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils;
using ZWave.BasicApplication.Devices;

namespace hyper
{
    public class Program
    {
        public static Controller controller;
        public static List<ConfigItem> configList;
        public static ProgramConfig programConfig;

        private static void SetupInputs(InputManager inputManager)
        {
            var tcpTarget = new TCPInput(5432)
            {
                Layout = @"${longdate} ${uppercase:${level}} ${message}"
            };
            var consoleTarget = new ConsoleInput()
            {
                Layout = @"${longdate} ${uppercase:${level}} ${message}"
            };

            LoggingSetupHelper.SetupLogging(tcpTarget, consoleTarget);

            inputManager.AddInput(consoleTarget);
            inputManager.AddInput(tcpTarget);
            var udpInput = new UDPInput(54322);
            inputManager.AddInput(udpInput);

            consoleTarget.StartReadingInput();
        }



        private static void SetupOutputs()
        {
            var udpOutput = new UDPOutput("127.0.0.1", 54321);
            var databaseOutput = new DatabaseOutput("events.db");
            OutputManager.AddOutput(udpOutput);
            OutputManager.AddOutput(databaseOutput);
        }

        private static void Main(string[] args)
        {
            //Test Version
            //Console.WriteLine("The version of the currently executing assembly is: {0}",
            //typeof(Program).Assembly.GetName().Version);

            //return;
            InputManager inputManager = new InputManager();
            SetupInputs(inputManager);
            SetupOutputs();

            ICommand currentCommand = null;

            Common.logger.Info("==== ZWave Hyper Hyper 5000 ====");
            Common.logger.Info("-----------------------------------");
            Common.logger.Info("Loading device configuration database...");
            if (!File.Exists("config.yaml"))
            {
                Common.logger.Error("configuration file config.yaml does not exist!");
                return;
            }
            var config = Common.ParseConfig("config.yaml");
            if (config == null)
            {
                Common.logger.Error("Could not parse configuration file config.yaml!");
                return;
            }
            Program.configList = config;
            Common.logger.Info("Got configuration for " + config.Count + " devices.");

            programConfig = new ProgramConfig();
            programConfig.LoadFromFile();

            Common.logger.Info("-----------------------------------");

            var startArgs = new StartArguments(args);
            if (!startArgs.Valid)
            {
                startArgs.PrintUsage();
                return;
            }

            Controller controller = null;
            string errorMessage = "";

            var port = startArgs.Port;
            bool startUdpMultiplexer = startArgs.StartUdpMultiplexer;
            bool initialized = false;
            bool simulationMode = false;

            if (port == StartArguments.AutoPort)
            {
                //for easier debugging, not meant to be used in production
                initialized = Common.InitControllerAuto(startUdpMultiplexer, out controller, out errorMessage);
                if (initialized)
                {
                    port = errorMessage;
                }
            }
            else if (port == StartArguments.SimulatePort)
            {
                simulationMode = true;
                initialized = true;
            }
            else
            {
                initialized = Common.InitController(port, startUdpMultiplexer, out controller, out errorMessage);
            }
            if (!initialized)
            {
                Common.logger.Error("Error connecting with port {0}! Error Mesage:", port);
                Common.logger.Error(errorMessage);
                return;
            }
            if (!simulationMode)
            {
                Program.controller = controller;
                Common.logger.Info("Version: {0}", controller.Version);
                // The HomeId identifying the z-wave network.
                // it should be unique for each controller.
                Common.logger.Info("HomeId: {0}", Tools.GetHex(controller.HomeId));
                InteractiveCommand.LogIncludedNodes();
                Common.logger.Info("-----------------------------------");
            }

            var deleteTimer = new DatabaseDeleteTimer(programConfig);
            deleteTimer.Start();

            currentCommand = new InteractiveCommand(startArgs.Command, inputManager, port);
            currentCommand.Start();

            /*if (args[1] == "r" || args[1] == "replace" || args[1] == "c" || args[1] == "config")
            {
                if (args.Length != 3)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");
                    if (args[1] == "r" || args[1] == "replace")
                    {
                        Common.logger.Info("./hyper [serialPort] r [nodeid]");
                    }
                    else
                    {
                        Common.logger.Info("./hyper [serialPort] c [nodeid]");
                    }

                    return;
                }

                if (!byte.TryParse(args[2], out byte nodeId))
                {
                    Common.logger.Info("argument 1 should be node id! " + args[2] + " is not a number!");
                    return;
                }

                if (!controller.IncludedNodes.Contains(nodeId))
                {
                    Common.logger.Info("NodeID " + nodeId + " not included in network!");
                    Common.logger.Info(string.Join(", ", controller.IncludedNodes));
                    //    return;
                }

                if (args[1] == "r" || args[1] == "replace")
                {
                    new ReplaceCommand(controller, nodeId, config).Start();
                }
                else
                {
                    new ConfigCommand(controller, nodeId, config).Start();
                }
            }
            else if (args[1] == "i" || args[1] == "include")
            {
                if (args.Length != 2)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] i");

                    return;
                }

                new IncludeCommand(controller, config).Start();
            }
            else if (args[1] == "e" || args[1] == "exclude")
            {
                if (args.Length != 2)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] e");

                    return;
                }

                new ExcludeCommand(controller).Start();
            }
            else if (args[1] == "l" || args[1] == "listen")
            {
                if (args.Length != 2)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] l");

                    return;
                }
                currentCommand = new ListenCommand(controller, config);
                currentCommand.Start();
            }
            else if (args[1] == "p" || args[1] == "ping")
            {
                if (args.Length != 3)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] p [nodeid]");

                    return;
                }

                if (!byte.TryParse(args[2], out byte nodeId))
                {
                    Common.logger.Info("argument should be node id! " + args[2] + " is not a number!");
                    return;
                }

                if (!controller.IncludedNodes.Contains(nodeId))
                {
                    Common.logger.Info("NodeID " + nodeId + " not included in network!");
                    Common.logger.Info(string.Join(", ", controller.IncludedNodes));
                    return;
                }

                currentCommand = new PingCommand(controller, nodeId);
                currentCommand.Start();
            }
            else if (args[1] == "rc" || args[1] == "reconfigure")
            {
                if (args.Length != 2)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] rc");

                    return;
                }
                currentCommand = new ReconfigureCommand(controller, config);
                currentCommand.Start();
            }
            else if (args[1] == "it" || args[1] == "interactive")
            {
                currentCommand = new InteractiveCommand(controller, config);
                currentCommand.Start();
            }
            else if (args[1] == "fr" || args[1] == "forceRemove")
            {
                if (args.Length != 3)
                {
                    Common.logger.Info("wrong arguments!");
                    Common.logger.Info("correct usage:");

                    Common.logger.Info("./hyper [serialPort] fr [nodeid]");

                    return;
                }

                if (!byte.TryParse(args[2], out byte nodeId))
                {
                    Common.logger.Info("argument should be node id! " + args[2] + " is not a number!");
                    return;
                }

                if (!controller.IncludedNodes.Contains(nodeId))
                {
                    Common.logger.Info("NodeID " + nodeId + " not included in network!");
                    Common.logger.Info(string.Join(", ", controller.IncludedNodes));
                    return;
                }

                currentCommand = new ForceRemoveCommand(controller, nodeId);
                currentCommand.Start();
            }
            else
            {
                Common.logger.Info("unknown command: {0}", args[1]);
                Common.logger.Info("valid commands:");
                Common.logger.Info("r/replace, c/config, i/include, e/exclude, l/listen, p/ping");
            }*/

            Common.logger.Info("----------");
            Common.logger.Info("Press any key to exit...");
            Console.ReadKey();
        }
    }
}