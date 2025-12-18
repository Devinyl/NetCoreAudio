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
        private double lastReportedPosition = -1;
        private bool durationSet = false;
        private long totalFrameCount = 0; // Total frames in the track
        private double totalDurationSeconds = 0; // Total duration in seconds
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        protected override string GetBashCommand(string fileName)
        {
            return "mpg123 -R";
        }
        

        public override async Task Play(string fileName)
        {
            //await Stop();
            if(!IsRunning())
                StartMpg123();

            // Reset duration tracking for new file
            durationSet = false;
            lastReportedPosition = -1;
            totalFrameCount = 0;
            totalDurationSeconds = 0;
            totalDurationSeconds = 0;
            
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
            _process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"mpg123 stderr: {e.Data}");
                }
            };
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
                        // Parse @F frame: @F <frame> <frames-left> <seconds> <seconds-left>
                        var parts = response.Split(' ');
                        if (parts.Length >= 5 
                            && long.TryParse(parts[1], out long currentFrame)
                            && long.TryParse(parts[2], out long framesLeft)
                            && double.TryParse(parts[3], out double currentSeconds) 
                            && double.TryParse(parts[4], out double secondsLeft))
                        {
                            // Set duration once from the first @F frame
                            if (!durationSet)
                            {
                                totalDurationSeconds = currentSeconds + secondsLeft;
                                // Calculate total frame count based on the actual position
                                // Don't use currentFrame + framesLeft as currentFrame is absolute position
                                // Instead, derive from frame position and time position
                                if (currentSeconds > 0)
                                {
                                    // Calculate frames per second from current data
                                    double fps = currentFrame / currentSeconds;
                                    totalFrameCount = (long)(fps * totalDurationSeconds);
                                }
                                else
                                {
                                    // Fallback: use framesLeft if we're at the start
                                    totalFrameCount = framesLeft;
                                }
                                var totalDuration = TimeSpan.FromSeconds(totalDurationSeconds);
                                DurationChanged?.Invoke(this, totalDuration);
                                durationSet = true;
                                Console.WriteLine($"Track info: {totalFrameCount} frames, {totalDurationSeconds:F1} seconds (calculated from frame {currentFrame} at {currentSeconds:F1}s)");
                            }
                            
                            // Check if track has finished (less than 1 second remaining)
                            if (secondsLeft < 1.0 && Playing)
                            {
                                Console.WriteLine($"Track finished: {currentSeconds:F1}s / {totalDurationSeconds:F1}s");
                                HandlePlaybackFinished(this, EventArgs.Empty);
                                Playing = false;
                            }
                            // Only report position updates once per second to avoid flooding
                            else if (Math.Abs(currentSeconds - lastReportedPosition) >= 1.0)
                            {
                                lastReportedPosition = currentSeconds;
                                var position = TimeSpan.FromSeconds(currentSeconds);
                                PositionChanged?.Invoke(this, position);
                                //Console.WriteLine($"Position: {currentSeconds:F1}s");
                            }
                        }
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
                        Console.WriteLine($"PlayingState: {State} (after seek: ignoring stopped state)");
                        // Don't trigger track finished on stopped state - it's unreliable
                        // We detect track finished from @F frames instead
                        // if(State == PlayingState.stopped) HandlePlaybackFinished(this, EventArgs.Empty);
                        break;
                    case string r when response.StartsWith(@"@E"):
                        Console.WriteLine($"mpg123 ERROR: {response}");
                        break;
                    default:
                        Console.WriteLine($"mpg123: {response}");
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
                    // Don't trigger track finished here - we handle it from @F frames
                    // This stopped state can be triggered by seeking or other operations
                    Console.WriteLine("HandlePlayEvents: stopped state ignored (handled via @F frames)");
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

        public override async Task Seek(long position)
        {
            if (IsRunning() && totalFrameCount > 0 && totalDurationSeconds > 0)
            {
                // Calculate frame position based on actual total frame count
                // This handles variable bitrate MP3s correctly
                double framesPerSecond = totalFrameCount / totalDurationSeconds;
                long targetFrame = (long)(position * framesPerSecond);
                
                // Ensure we don't seek beyond the end of the track
                if (targetFrame >= totalFrameCount)
                {
                    Console.WriteLine($"Seek position {position}s (frame {targetFrame}) is beyond track end ({totalFrameCount} frames), skipping");
                    return;
                }
                
                Console.WriteLine($"Seeking to {position}s using actual fps {framesPerSecond:F1} (frame {targetFrame}/{totalFrameCount})");
                this.SendCommand($"JUMP {targetFrame}");
                Console.WriteLine($"mpg123 seek command sent: JUMP {targetFrame}");
            }
            else
            {
                Console.WriteLine($"Cannot seek: track not ready (frames={totalFrameCount}, duration={totalDurationSeconds}s)");
            }
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
