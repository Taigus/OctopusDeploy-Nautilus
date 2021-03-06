﻿using System;
using CommandLine;

namespace Nautilus
{
    public abstract class CommandBase
    {
        [Option('s', "server", Required = true, HelpText = "Octopus server address (e.g. http://your-octopus-server/).")]
        public string OctopusServerAddress { get; set; }

        [Option('k', "apikey", Required = true, HelpText = "Octopus API key.")]
        public string OctopusApiKey { get; set; }
                
        public int Run()
        {            
            var octopus = new OctopusProxy(OctopusServerAddress, OctopusApiKey);
            return Run(octopus);
        }
        
        protected abstract int Run(OctopusProxy octopus);
        
        protected static void Write(string value, ConsoleColor? color = null)
        {
            Write(value, color, Console.Write);
        }
        
        protected static void WriteLine(string value = null, ConsoleColor? color = null)
        {
            Write(value, color, Console.WriteLine);
        }
        
        private static void Write(string value, ConsoleColor? color, Action<string> write)
        {
            if (color.HasValue)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                write(value);
                Console.ForegroundColor = oldColor;
            }
            else
            {
                write(value);
            }
        }
    }    
}
