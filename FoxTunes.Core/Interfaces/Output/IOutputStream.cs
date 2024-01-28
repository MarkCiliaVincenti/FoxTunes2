﻿using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOutputStream : IBaseComponent, IDisposable
    {
        int Id { get; }

        string FileName { get; }

        PlaylistItem PlaylistItem { get; }

        long Position { get; set; }

        long Length { get; }

        int Rate { get; }

        int Channels { get; }

        bool IsPlaying { get; }

        bool IsPaused { get; }

        bool IsStopped { get; }

        Task Play();

        Task Pause();

        Task Resume();

        Task Stop();

        event EventHandler Ending;

        event EventHandler Ended;

        string Description { get; }

        Task BeginSeek();

        Task EndSeek();

        TimeSpan GetDuration(long position);

        bool CanReset { get; }

        void Reset();

        bool IsDisposed { get; }
    }
}
