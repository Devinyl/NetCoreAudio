using System;
using System.Threading.Tasks;
using NetCoreAudio.Utils;

namespace NetCoreAudio.Interfaces
{
    public interface IPlayer : IDisposable
    {
        event EventHandler PlaybackFinished;
        // Semantic event: raised when the current track reaches its natural end
        // (not when the underlying process exits or emits warnings). This should
        // be used by higher layers to advance playlists reliably.
        event EventHandler TrackFinished;
        event EventHandler<TimeSpan> PositionChanged;
        event EventHandler<TimeSpan> DurationChanged;

        bool Playing { get; }
        bool Paused { get; }

        Task Play(string fileName);
        Task Pause();
        Task Resume();
        Task Stop();
        Task SetVolume(byte percent);
        Task<AudioFileInfo> GetFileInfo();
        Task<long> GetStatus();

        Task Seek(long position);
    }
}
