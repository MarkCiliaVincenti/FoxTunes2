﻿using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassStreamFactory : StandardComponent, IBassStreamFactory
    {
        public BassStreamFactory()
        {
            this.Advisors = new List<IBassStreamAdvisor>();
            this.Providers = new List<IBassStreamProvider>();
        }

        private List<IBassStreamAdvisor> Advisors { get; set; }

        private List<IBassStreamProvider> Providers { get; set; }

        IEnumerable<IBassStreamAdvisor> IBassStreamFactory.Advisors
        {
            get
            {
                return this.Advisors;
            }
        }

        IEnumerable<IBassStreamProvider> IBassStreamFactory.Providers
        {
            get
            {
                return this.Providers;
            }
        }

        public IBassOutput Output { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output as IBassOutput;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        public void Register(IBassStreamAdvisor advisor)
        {
            this.Advisors.Add(advisor);
            Logger.Write(this, LogLevel.Debug, "Registered bass stream advisor \"{0}\".", advisor.GetType().Name);
        }

        public void Register(IBassStreamProvider provider)
        {
            this.Providers.Add(provider);
            Logger.Write(this, LogLevel.Debug, "Registered bass stream provider \"{0}\".", provider.GetType().Name);
        }

        public IEnumerable<IBassStreamAdvice> GetAdvice(IBassStreamProvider provider, PlaylistItem playlistItem)
        {
            var advice = new List<IBassStreamAdvice>();
            foreach (var advisor in this.Advisors)
            {
                advisor.Advise(provider, playlistItem, advice);
            }
            return advice.ToArray();
        }

        public IEnumerable<IBassStreamProvider> GetProviders(PlaylistItem playlistItem)
        {
            return this.Providers.Where(
                provider => provider.CanCreateStream(playlistItem)
            ).ToArray();
        }

        public IBassStream CreateBasicStream(PlaylistItem playlistItem, BassFlags flags)
        {
            flags |= BassFlags.Decode;
            Logger.Write(this, LogLevel.Debug, "Attempting to create stream for file \"{0}\".", playlistItem.FileName);
            var provider = this.GetProviders(playlistItem).FirstOrDefault();
            if (provider == null)
            {
                Logger.Write(this, LogLevel.Warn, "No provider was found for file \"{0}\".", playlistItem.FileName);
                return BassStream.Empty;
            }
            Logger.Write(this, LogLevel.Debug, "Using bass stream provider \"{0}\".", provider.GetType().Name);
            var advice = this.GetAdvice(provider, playlistItem).ToArray();
            var stream = provider.CreateBasicStream(playlistItem, advice, flags);
            if (stream.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Created stream from file {0}: {1}", playlistItem.FileName, stream.ChannelHandle);
                return stream;
            }
            if (stream.Errors == Errors.Already && provider.Flags.HasFlag(BassStreamProviderFlags.Serial))
            {
                Logger.Write(this, LogLevel.Debug, "Provider does not support multiple streams.");
                return stream;
            }
            Logger.Write(this, LogLevel.Debug, "Failed to create stream from file {0}: {1}", playlistItem.FileName, Enum.GetName(typeof(Errors), stream.Errors));
            return stream;
        }

        public IBassStream CreateInteractiveStream(PlaylistItem playlistItem, bool immidiate, BassFlags flags)
        {
            flags |= BassFlags.Decode;
            Logger.Write(this, LogLevel.Debug, "Attempting to create stream for file \"{0}\".", playlistItem.FileName);
            var provider = this.GetProviders(playlistItem).FirstOrDefault();
            if (provider == null)
            {
                Logger.Write(this, LogLevel.Warn, "No provider was found for file \"{0}\".", playlistItem.FileName);
                return BassStream.Empty;
            }
            Logger.Write(this, LogLevel.Debug, "Using bass stream provider \"{0}\".", provider.GetType().Name);
            var advice = this.GetAdvice(provider, playlistItem).ToArray();
            var stream = provider.CreateInteractiveStream(playlistItem, advice, flags);
            if (stream.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Created stream from file {0}: {1}", playlistItem.FileName, stream.ChannelHandle);
                return stream;
            }
            if (stream.Errors == Errors.Already && provider.Flags.HasFlag(BassStreamProviderFlags.Serial))
            {
                if (immidiate)
                {
                    Logger.Write(this, LogLevel.Debug, "Provider does not support multiple streams but immidiate playback was requested, releasing active streams.");
                    if (BassOutputStreams.Clear())
                    {
                        Logger.Write(this, LogLevel.Debug, "Active streams were released, retrying.");
                        stream = provider.CreateInteractiveStream(playlistItem, advice, flags);
                        if (stream.ChannelHandle != 0)
                        {
                            Logger.Write(this, LogLevel.Debug, "Created stream from file {0}: {1}", playlistItem.FileName, stream.ChannelHandle);
                            return stream;
                        }
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Failed to release active streams.");
                    }
                }
                else
                {
                    return stream;
                }
            }
            Logger.Write(this, LogLevel.Debug, "Failed to create stream from file {0}: {1}", playlistItem.FileName, Enum.GetName(typeof(Errors), stream.Errors));
            return stream;
        }
    }
}
