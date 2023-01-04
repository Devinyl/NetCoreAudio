using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NetCoreAudio.Interfaces;

namespace NetCoreAudio.Players
{
    internal class LinuxPlayer : UnixPlayerBase, IPlayer
    {
        protected override string GetBashCommand(string fileName)
        {
            if (Path.GetExtension(fileName).ToLower().Equals(".mp3"))
            {
                return "mpg123 -q";
            }
            else
            {
                return "aplay -q";
            }
        }

        public override async Task Play(string fileName)
        {
            await Stop();
            var BashToolName = GetBashCommand(fileName);
            fileName = fileName.Replace("\"",  "\\\"");
            /*fileName = Regex.Replace(fileName, @"(\\+)$", @"$1$1").Replace("'", @"\'").Replace(" ", @"\ "); */
            Console.WriteLine($"{BashToolName} \"{fileName}\"");
            _process = StartBashProcess($"{BashToolName} \"{fileName}\"");
            _process.EnableRaisingEvents = true;
            _process.Exited += HandlePlaybackFinished;
            _process.ErrorDataReceived += HandlePlaybackFinished;
            _process.Disposed += HandlePlaybackFinished;
            Playing = true;

            _audioFileInfo.FilePath = fileName;
            _audioFileInfo.FileName = System.IO.Path.GetFileName(fileName);
            _audioFileInfo.FileExtension = System.IO.Path.GetExtension(fileName);
            _audioFileInfo.FileSize = new System.IO.FileInfo(fileName).Length;
        }

        protected override Process StartBashProcess(string command)
        {
            var escapedArgs = command.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            //OpenCtrlInterface(process.StandardOutput.BaseStream);
            return process;
        }
                
        internal async Task OpenCtrlInterface(Stream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            while(true)
            {
                var line = await reader.ReadLineAsync();
                if(line == null)
                    Task.Delay(50);
                else
                    Console.WriteLine(line);
            }
        }

        public override Task SetVolume(byte percent)
        {
            if (percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent), "Percent can't exceed 100");

            //var tempProcess = StartBashProcess($"amixer -M set 'Master' {percent}%");
            //tempProcess.WaitForExit();

            return Task.CompletedTask;
        }
    }
}
