using NetCoreAudio.Interfaces;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using NetCoreAudio.Utils;

namespace NetCoreAudio.Players
{
    internal abstract class UnixPlayerBase : IPlayer
    {
        internal Process _process = null;

        internal const string PauseProcessCommand = "kill -STOP {0}";
        internal const string ResumeProcessCommand = "kill -CONT {0}";

        public event EventHandler PlaybackFinished;
        
        internal AudioFileInfo _audioFileInfo = new AudioFileInfo();

        public bool Playing { get; set; }

        public bool Paused { get; set; }

        protected abstract string GetBashCommand(string fileName);

        public virtual async Task Play(string fileName)
        {
            //await Stop();
            //var BashToolName = GetBashCommand(fileName);
            //fileName = fileName.Replace("\"",  "\\\"");
            ///*fileName = Regex.Replace(fileName, @"(\\+)$", @"$1$1").Replace("'", @"\'").Replace(" ", @"\ "); */
            //Console.WriteLine($"{BashToolName} \"{fileName}\"");
            //_process = StartBashProcess($"{BashToolName} \"{fileName}\"");
            //_process.EnableRaisingEvents = true;
            //_process.Exited += HandlePlaybackFinished;
            //_process.ErrorDataReceived += HandlePlaybackFinished;
            //_process.Disposed += HandlePlaybackFinished;
            //Playing = true;
            //
            //_audioFileInfo.FilePath = fileName;
            //_audioFileInfo.FileName = System.IO.Path.GetFileName(fileName);
            //_audioFileInfo.FileExtension = System.IO.Path.GetExtension(fileName);
            //_audioFileInfo.FileSize = new System.IO.FileInfo(fileName).Length;
        }

        public virtual Task Pause()
        {
            if (Playing && !Paused && _process != null)
            {
                var tempProcess = StartBashProcess(string.Format(PauseProcessCommand, _process.Id));
                tempProcess.WaitForExit();
                Paused = true;
            }

            return Task.CompletedTask;
        }

        public virtual Task Resume()
        {
            if (Playing && Paused && _process != null)
            {
                var tempProcess = StartBashProcess(string.Format(ResumeProcessCommand, _process.Id));
                tempProcess.WaitForExit();
                Paused = false;
            }

            return Task.CompletedTask;
        }

        public virtual Task Stop()
        {
            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }

            Playing = false;
            Paused = false;

            return Task.CompletedTask;
        }

        protected virtual Process StartBashProcess(string command)
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
            return process;
        }

        public Task<AudioFileInfo> GetFileInfo()
        {
            return new Task<AudioFileInfo>(() => _audioFileInfo);
        }

        public Task<long> GetStatus()
        {
            return new Task<long>(() => 0);
        }

        public Task Seek(long position)
        {
            return Task.CompletedTask;
        }

        internal void HandlePlaybackFinished(object sender, EventArgs e)
        {
            if (Playing)
            {
                Playing = false;
                PlaybackFinished?.Invoke(this, e);
            }
        }

        public abstract Task SetVolume(byte percent);

        public void Dispose()
        {
            Stop().Wait();
        }
    }
}
