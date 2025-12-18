using NetCoreAudio.Interfaces;
using NetCoreAudio.Players;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NetCoreAudio.Utils;

namespace NetCoreAudio
{
    public class Player : IPlayer
    {
        private readonly IPlayer _internalPlayer;

        /// <summary>
        /// Internally, sets Playing flag to false. Additional handlers can be attached to it to handle any custom logic.
        /// </summary>
        public event EventHandler PlaybackFinished;

        /// <summary>
        /// Raised when the playback position changes during playback.
        /// </summary>
        public event EventHandler<TimeSpan> PositionChanged;

        /// <summary>
        /// Raised when the track duration is determined.
        /// </summary>
        public event EventHandler<TimeSpan> DurationChanged;

        /// <summary>
        /// Indicates that the audio is currently playing.
        /// </summary>
        public bool Playing => _internalPlayer.Playing;

        /// <summary>
        /// Indicates that the audio playback is currently paused.
        /// </summary>
        public bool Paused => _internalPlayer.Paused;

        public Player()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _internalPlayer = new WindowsPlayer();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _internalPlayer = new LinuxPlayer();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _internalPlayer = new MacPlayer();
            else
                throw new Exception("No implementation exist for the current OS");

            _internalPlayer.PlaybackFinished += OnPlaybackFinished;
            _internalPlayer.PositionChanged += OnPositionChanged;
            _internalPlayer.DurationChanged += OnDurationChanged;
        }

        /// <summary>
        /// Will stop any current playback and will start playing the specified audio file. The fileName parameter can be an absolute path or a path relative to the directory where the library is located. Sets Playing flag to true. Sets Paused flag to false.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task Play(string fileName)
        {
            await _internalPlayer.Play(fileName);
        }

        /// <summary>
        /// Pauses any ongong playback. Sets Paused flag to true. Doesn't modify Playing flag.
        /// </summary>
        /// <returns></returns>
        public async Task Pause()
        {
            await _internalPlayer.Pause();
        }

        /// <summary>
        /// Resumes any paused playback. Sets Paused flag to false. Doesn't modify Playing flag.
        /// </summary>
        /// <returns></returns>
        public async Task Resume()
        {
            await _internalPlayer.Resume();
        }

        /// <summary>
        /// Stops any current playback and clears the buffer. Sets Playing and Paused flags to false.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await _internalPlayer.Stop();
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            PlaybackFinished?.Invoke(this, e);
        }

        private void OnPositionChanged(object sender, TimeSpan position)
        {
            PositionChanged?.Invoke(this, position);
        }

        private void OnDurationChanged(object sender, TimeSpan duration)
        {
            DurationChanged?.Invoke(this, duration);
        }

        /// <summary>
        /// Sets the playing volume as percent
        /// </summary>
        /// <returns></returns>
        public async Task SetVolume(byte percent)
        {
            await _internalPlayer.SetVolume(percent);
        }

        /// <summary>
        /// Returns media and file information
        /// </summary>
        /// <returns></returns>
        public async Task<AudioFileInfo> GetFileInfo()
        {
            return await _internalPlayer.GetFileInfo();
        }

        /// <summary>
        /// Returns the current position of the playback
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetStatus()
        {
            return await _internalPlayer.GetStatus();
        }

        public async Task Seek(long position)
        {
            await _internalPlayer.Seek(position);
        }

        public void Dispose()
        {
            _internalPlayer.Dispose();
        }
    }
}
