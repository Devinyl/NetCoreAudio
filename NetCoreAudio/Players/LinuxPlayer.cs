using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NetCoreAudio.Interfaces;

namespace NetCoreAudio.Players
{
    internal class LinuxPlayer : UnixPlayerBase, IPlayer
    {
        enum PlayingState
        {
            stopped,
            paused,
            unpaused,
            stopping,
            pausing,
            unpausing
        }
        private PlayingState State { get; set; }
        private StreamWriter ctrlStream;
        
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        private byte volume = 25;

        private int counter;
        protected override string GetBashCommand(string fileName)
        {
            return "mpg123 -R";
        }
        

        public override async Task Play(string fileName)
        {
            //await Stop();
            if(!IsRunning())
                StartMpg123();

            SendCommand("L " + fileName);
            SetVolume(volume);
            Playing = true;
            Paused = false;
        }

        protected bool IsRunning() => (this.ctrlStream != null && this._process != null);

        protected void StartMpg123()
        {
            var BashToolName = GetBashCommand("");
            _process = StartBashProcess($"{BashToolName}");
            _process.EnableRaisingEvents = true;
            _process.Exited += HandlePlaybackFinished;
            _process.ErrorDataReceived += HandlePlaybackFinished;
            _process.Disposed += HandlePlaybackFinished;
            Playing = true;
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
            process.OutputDataReceived += DataReceivedHandler;
            process.Start();
            ctrlStream = process.StandardInput;
            process.BeginOutputReadLine();
            return process;
        }

        private bool SendCommand(string cmd)
        {
            if(IsRunning())
            {
                this.ctrlStream.WriteLine(cmd);
                return true;
            }
            return false;

        }

        private void DataReceivedHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                var response = outLine.Data;
                switch(response)
                {
                    case string r when response.StartsWith(@"@F"):
                        counter++;
                        //Console.WriteLine(response);
                        //if( (counter % 40) == 0)
                        //    Console.WriteLine(response.Split(" ")[3]);
                        break;
                    case string r when response.StartsWith(@"@I"):
                        Console.WriteLine(r);
                        break;
                    // case @"@P 0":
                    //     Console.WriteLine(response + " Got PlayBackFinisehd Event from MPg123");
                    //     //HandlePlaybackFinished(this, EventArgs.Empty);
                    //     break;
                    case string r when response.StartsWith(@"@P"):
                        var code = response.Split(" ")[1];
                        State = (PlayingState) int.Parse(code);
                        Console.WriteLine("PlayingState: " + State.ToString());
                        if(State == PlayingState.stopped) HandlePlaybackFinished(this, EventArgs.Empty);
                        break;
                    default:
                        Console.WriteLine(response);
                        break;
                }
                // complete task in event
            }
        }

        private void HandlePlayEvents(PlayingState state)
        {
            switch (State)
            {
                case PlayingState.stopping:
                    tcs.SetResult(true);
                    break;
                case PlayingState.stopped:
                    if (this.State == PlayingState.stopping)
                    {
                        this.State = PlayingState.stopped;
                        HandlePlaybackFinished(this, EventArgs.Empty);
                    }
                    break;
                case PlayingState.pausing:
                    break;
                default:
                    break;
            }
        }
        public async override Task Pause()
        {
            if (IsRunning())
                this.SendCommand("P");
            
            //await tcs.Task;
        }

        public async override Task Resume()
        {
            if (IsRunning())
                this.SendCommand("P");

            //await tcs.Task;
        }

        public async override Task SetVolume(byte percent)
        {
            if (percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent), "Percent can't exceed 100");

            this.volume = percent;
            this.SendCommand("V " + percent);

            //await tcs.Task;
        }
        
        public async override Task Stop()
        {
            this.State = PlayingState.stopping;
            tcs = new TaskCompletionSource<bool>();
            
            //Send Pause Signal
            this.SendCommand("S");

            await tcs.Task;
            
            Playing = false;
            Paused = false;
        }
        
        public void Dispose()
        {
            if (this.ctrlStream != null)
                this.ctrlStream.Dispose();
            
            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }
        }
    }
}
