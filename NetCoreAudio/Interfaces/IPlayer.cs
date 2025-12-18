using System;
using System.Threading.Tasks;
using NetCoreAudio.Utils;

namespace NetCoreAudio.Interfaces
{
    public interface IPlayer : IDisposable
    {
        event EventHandler PlaybackFinished;
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
