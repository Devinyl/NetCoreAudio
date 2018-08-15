﻿using NetCoreAudio;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DemoHarness
{
    class Program
    {
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command, StringBuilder stringReturn, int returnLength, IntPtr hwndCallback);

        static void Main(string[] args)
        {
            var player = new Player();

            Console.WriteLine("Welcome to the demo of NetCoreAudio package");
            Console.WriteLine("Please enter the full path to the file you would like to play:");
            var fileName = Console.ReadLine();
            Console.WriteLine("You can manipulate the player with the following commands:");
            Console.WriteLine("play - Play the specified file from the start");
            Console.WriteLine("pause - Pause the playback");
            Console.WriteLine("resume - Resume the playback");
            Console.WriteLine("stop - Stop the playback");
            Console.WriteLine("exit - Exit the app");

            while (true)
            {
                var command = Console.ReadLine();

                try
                {
                    switch (command)
                    {
                        case "play":
                            player.Play(fileName).Wait();
                            Console.WriteLine("Playback started");
                            break;
                        case "pause":
                            player.Pause().Wait();
                            Console.WriteLine("Playback paused");
                            break;
                        case "resume":
                            player.Resume().Wait();
                            Console.WriteLine("Playback resumed");
                            break;
                        case "stop":
                            player.Stop().Wait();
                            Console.WriteLine("Playback stopped");
                            break;
                        case "exit":
                            break;
                        default:
                            Console.WriteLine("Haven't got a clue, mate!");
                            break;
                    }

                    if (command == "exit") break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
