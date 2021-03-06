using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using CommandLine;

namespace Nautilus
{    
    [Verb("install", HelpText = "Installs an Octopus Tentacle on the local machine.")]
    public class InstallCommand : CommandBase
    {
        [Option('l', "location", Required = false, HelpText = "The install directory of the Octopus Tentacle. Defaults to Program Files.")]
        public string InstallLocation { get; set; }
        
        [Option('h', "home", Required = false, HelpText = "The home directory of the Octopus Tentacle. Defaults to \"C:\\Octopus\"")]
        public string HomeLocation { get; set; }
        
        [Option('t', "thumbprint", Required = false, HelpText = "The Octopus Server thumbprint. Defaults to global certificate thumbprint.")]
        public string Thumbprint { get; set; }      
                
        [Option('p', "port", Required = false, HelpText = "The port of the Octopus Tentacle. Defaults to 10933.")]
        public int? Port { get; set; }
        
        protected override int Run(OctopusProxy octopus)
        {            
            var installLocation = InstallLocation ?? Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), @"Octopus Deploy\Tentacle");
            
            var homeLocation = HomeLocation ?? @"C:\Octopus";
            
            var thumbprint = Thumbprint;
            if (thumbprint == null)
            {
                var certicate = octopus.GetGlobalCertificate();
                thumbprint = certicate.Thumbprint;
            }
                        
            var port = Port ?? 10933;
            
            var systemInfo = octopus.GetSystemInfo();        
            var downloadVersion = systemInfo.Version;            
            if (Environment.Is64BitOperatingSystem)
            {
                downloadVersion += "-x64";
            }            
            var downloadUrl = $"http://download.octopusdeploy.com/octopus/Octopus.Tentacle.{downloadVersion}.msi";            
            var filePath = $"{Path.GetTempPath()}Octopus.Tentacle.{downloadVersion}.msi";
            
            Write($"Downloading installer from {downloadUrl} to {filePath}... ");
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(downloadUrl, filePath);
            }
            WriteLine("done");       
             
            Write($"Installing tentacle from {filePath}... ");
            if (RunProcess("msiexec", $"INSTALLLOCATION=\"{installLocation}\" /i \"{filePath}\" /quiet"))
            {                      
                WriteLine("done");
                
                Write("Configuring tentacle... ");            
                var tentacleExe = installLocation + @"\Tentacle.exe";            
                
                if (RunProcess(tentacleExe, $"create-instance --instance \"Tentacle\" --config \"{homeLocation}\\Tentacle.config\" --console"))
                if (RunProcess(tentacleExe, $"new-certificate --instance \"Tentacle\" --if-blank --console"))
                if (RunProcess(tentacleExe, $"configure --instance \"Tentacle\" --reset-trust --console"))
                if (RunProcess(tentacleExe, $"configure --instance \"Tentacle\" --home \"{homeLocation}\" --app \"{homeLocation}\\Applications\" --port \"{port}\" --console"))
                if (RunProcess(tentacleExe, $"configure --instance \"Tentacle\" --trust \"{thumbprint}\" --console"))
                if (RunProcess("netsh", $"advfirewall firewall add rule \"name=Octopus Deploy Tentacle\" dir=in action=allow protocol=TCP localport={port}"))
                if (RunProcess(tentacleExe, $"service --instance \"Tentacle\" --install --start --console"))
                {
                    WriteLine("done");
                    return 0;
                }
            }      
            
            return 1;
        }
        
        private static bool RunProcess(string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(startInfo))
            {
                const int timeout = 120000;
                if (!process.WaitForExit(timeout))
                {
                    WriteLine("failed", ConsoleColor.Red);
                    WriteLine($"Error: \"{fileName} {arguments}\" operation timed out ({timeout} milliseconds)");
                    return false;
                }
                
                if (process.ExitCode != 0)
                {                    
                    WriteLine("failed", ConsoleColor.Red);
                    WriteLine($"Error: \"{fileName} {arguments}\" operation failed and exited with code {process.ExitCode}");
                    return false;
                }
                
                return true;
            }
        }
    }
}