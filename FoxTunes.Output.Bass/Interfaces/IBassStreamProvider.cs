﻿using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamProvider : IBaseComponent, IDisposable
    {
        BassStreamProviderFlags Flags { get; }

        bool CanCreateStream(PlaylistItem playlistItem);

        IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags);

        IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags);

        long GetPosition(int channelHandle);

        void SetPosition(int channelHandle, long value);

        void FreeStream(PlaylistItem playlistItem, int channelHandle);
    }

    [Flags]
    public enum BassStreamProviderFlags : byte
    {
        None = 0,
        Serial = 1
    }
}
