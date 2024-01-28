﻿using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Asio;
using ManagedBass.Gapless.Asio;
using ManagedBass.Sox;
using ManagedBass.Sox.Asio;
using System;
using System.Linq;
using System.Threading;

namespace FoxTunes
{
    public class BassAsioOutputChannel : BassOutputChannel
    {
        const int START_ATTEMPTS = 5;

        const int START_ATTEMPT_INTERVAL = 400;

        const int PRIMARY_CHANNEL = 0;

        const int SECONDARY_CHANNEL = 1;

        const int SOX_BUFFER_LENGTH = 3;

        const bool SOX_BACKGROUND = true;

        public BassAsioOutputChannel(BassOutput output) : base(output)
        {

        }

        public bool DsdDirect { get; private set; }

        public AsioInitFlags AsioFlags
        {
            get
            {
                return AsioInitFlags.Thread;
            }
        }

        public AsioSampleFormat PCMFormat
        {
            get
            {
                if (this.Output.Float)
                {
                    return AsioSampleFormat.Float;
                }
                return AsioSampleFormat.Bit16;
            }
        }

        public AsioSampleFormat DSDFormat
        {
            get
            {
                return AsioSampleFormat.DSD_MSB;
            }
        }

        public override BassFlags OutputFlags
        {
            get
            {
                return base.OutputFlags | BassFlags.Decode;
            }
        }

        public override bool CanPlayDSD
        {
            get
            {
                return true;
            }
        }

        protected override bool CheckRate(int rate)
        {
            return BassAsio.CheckRate(rate);
        }

        public override int BufferLength
        {
            get
            {
                if (this.IsResampling)
                {
                    return SOX_BUFFER_LENGTH;
                }
                return base.BufferLength;
            }
        }

        public override bool CheckFormat(int rate, int channels)
        {
            if (!this.CheckRate(channels) || channels > BassAsio.Info.Outputs)
            {
                return false;
            }
            return base.CheckFormat(rate, channels);
        }

        protected override void CreateChannel()
        {
            Logger.Write(this, LogLevel.Debug, "Initializing BASS ASIO.");
            BassUtils.OK(BassAsio.Init(this.Output.AsioDevice, this.AsioFlags));
            if (this.Channels > BassAsio.Info.Outputs)
            {
                //TODO: We should down mix.
                Logger.Write(this, LogLevel.Error, "Cannot play stream with more channels than device outputs.");
                throw new NotImplementedException(string.Format("The stream contains {0} channels which is greater than {1} output channels provided by the device.", this.Channels, BassAsio.Info.Outputs));
            }
            Logger.Write(this, LogLevel.Debug, "Configuring BASS ASIO.");
            if (this.ShouldResample)
            {
                base.CreateChannel();
                Logger.Write(this, LogLevel.Debug, "Initializing BASS SOX ASIO.");
                BassUtils.OK(BassSoxAsio.Init());
                BassUtils.OK(BassSoxAsio.StreamSet(this.ResamplerChannelHandle));
                BassUtils.OK(BassSoxAsio.ChannelEnable(false, PRIMARY_CHANNEL));
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Initializing BASS GAPLESS ASIO.");
                BassUtils.OK(BassGaplessAsio.Init());
                BassUtils.OK(BassGaplessAsio.ChannelEnable(false, PRIMARY_CHANNEL));
            }
            if (this.Channels == 1)
            {
                //Upmix mono to stereo.
                BassUtils.OK(BassAsio.ChannelEnableMirror(SECONDARY_CHANNEL, false, PRIMARY_CHANNEL));
            }
            else
            {
                //Deinterleave multi channel.
                for (var channel = 1; channel < this.Channels; channel++)
                {
                    BassUtils.OK(BassAsio.ChannelJoin(false, channel, PRIMARY_CHANNEL));
                }
            }
            this.DsdDirect = false;
            if (this.Output.DsdDirect && this.InputFlags.HasFlag(BassFlags.DSDRaw))
            {
                if (this.ConfigureDSD())
                {
                    this.DsdDirect = true;
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to configure DSD RAW, falling back to PCM.");
                }
            }
            if (!this.DsdDirect)
            {
                this.ConfigurePCM();
            }
        }

        private bool ConfigurePCM()
        {
            Logger.Write(this, LogLevel.Debug, "Configuring PCM.");
            BassUtils.OK(BassAsio.SetDSD(false));
            if (this.IsResampling)
            {
                BassAsio.Rate = this.Output.Rate;
                BassUtils.OK(BassAsio.ChannelSetRate(false, PRIMARY_CHANNEL, this.Output.Rate));
            }
            else if (this.ShouldEnforceRate)
            {
                BassAsio.Rate = this.Output.Rate;
                BassUtils.OK(BassAsio.ChannelSetRate(false, PRIMARY_CHANNEL, this.PCMRate));
                this.IsEnforcingRate = true;
            }
            else
            {
                BassAsio.Rate = this.PCMRate;
                BassUtils.OK(BassAsio.ChannelSetRate(false, PRIMARY_CHANNEL, this.PCMRate));
            }
            Logger.Write(this, LogLevel.Debug, "PCM: Rate = {0}, Format = {1}", BassAsio.Rate, Enum.GetName(typeof(AsioSampleFormat), this.PCMFormat));
            BassUtils.OK(BassAsio.ChannelSetFormat(false, PRIMARY_CHANNEL, this.PCMFormat));
            return true;
        }

        private bool ConfigureDSD()
        {
            try
            {
                Logger.Write(this, LogLevel.Debug, "Configuring DSD RAW.");
                try
                {
                    BassUtils.OK(BassAsio.SetDSD(true));
                }
                catch
                {
                    //If we get here some drivers (at least Creative) will crash when BassAsio.Start is called.
                    //I can't find a way to prevent it but it seems to be related to the allocated buffer size
                    //not being what the driver *thinks* it is and over-flowing.
                    Logger.Write(this, LogLevel.Error, "Failed to enable DSD RAW on the device. Creative ASIO driver becomes unstable and usually crashes soon...");
                    return false;
                }
                if (!this.CheckRate(this.DSDRate))
                {
                    Logger.Write(this, LogLevel.Warn, "DSD rate {0} is unsupported.", this.DSDRate);
                    return false;
                }
                else
                {
                    BassAsio.Rate = this.DSDRate;
                }
                Logger.Write(this, LogLevel.Debug, "DSD: Rate = {0}, Format = {1}", BassAsio.Rate, Enum.GetName(typeof(AsioSampleFormat), this.DSDFormat));
                BassUtils.OK(BassAsio.ChannelSetFormat(false, PRIMARY_CHANNEL, this.DSDFormat));
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to configure DSD RAW: {0}", e.Message);
                return false;
            }
        }

        protected override void CreateResamplingChannel()
        {
            base.CreateResamplingChannel();
            Logger.Write(this, LogLevel.Debug, "Configuring BASS SOX: Buffer Length = {0}, Background = {1}", SOX_BUFFER_LENGTH, SOX_BACKGROUND);
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ResamplerChannelHandle, SoxChannelAttribute.BufferLength, this.BufferLength));
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ResamplerChannelHandle, SoxChannelAttribute.Background, SOX_BACKGROUND));
        }

        protected override void FreeChannel()
        {
            if (BassAsio.IsStarted)
            {
                Logger.Write(this, LogLevel.Debug, "Stopping ASIO.");
                BassAsio.Stop();
            }
            try
            {
                var flags =
                    AsioChannelResetFlags.Enable |
                    AsioChannelResetFlags.Join |
                    AsioChannelResetFlags.Format |
                    AsioChannelResetFlags.Rate;
                Logger.Write(this, LogLevel.Debug, "Resetting channel attributes.");
                for (var channel = 0; channel < BassAsio.Info.Outputs; channel++)
                {
                    BassAsio.ChannelReset(false, channel, flags);
                }
            }
            catch (Exception e)
            {
                //Nothing can be done.
                Logger.Write(this, LogLevel.Warn, "Failed to reset channel attributes: {0}", e.Message);
            }
            if (this.IsResampling)
            {
                base.FreeChannel();
                Logger.Write(this, LogLevel.Debug, "Releasing BASS SOX ASIO.");
                BassSoxAsio.Free();
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Releasing BASS GAPLESS ASIO.");
                BassGaplessAsio.Free();
            }
            Logger.Write(this, LogLevel.Debug, "Releasing BASS ASIO.");
            BassAsio.Free();
        }

        /// <remarks>
        /// Usually ASIO will start first time however one device I own has a bunch of relays 
        /// which need to click a few times when changing formats and ASIO will fail to start
        /// until the clicking stops. 
        /// Pretty weird.
        /// </remarks>
        protected virtual bool StartAsio()
        {
            for (var a = 1; a <= START_ATTEMPTS; a++)
            {
                Logger.Write(this, LogLevel.Debug, "Starting ASIO, attempt: {0}", a);
                try
                {
                    var success = BassAsio.Start(BassAsio.Info.PreferredBufferLength);
                    if (success)
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully started ASIO.");
                        return true;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to start ASIO: {0}", Enum.GetName(typeof(Errors), BassAsio.LastError));
                    }
                }
                catch (Exception e)
                {
                    //Nothing can be done.
                    Logger.Write(this, LogLevel.Warn, "Failed to start ASIO: {0}", e.Message);
                }
                Thread.Sleep(START_ATTEMPT_INTERVAL);
            }
            Logger.Write(this, LogLevel.Warn, "Failed to start ASIO after {0} attempts.", START_ATTEMPTS);
            return false;
        }

        public override bool CanPlay(BassOutputStream outputStream)
        {
            if (this.DsdDirect && !BassUtils.GetChannelFlags(outputStream.ChannelHandle).HasFlag(BassFlags.DSDRaw))
            {
                return false;
            }
            return base.CanPlay(outputStream);
        }

        public override bool IsPlaying
        {
            get
            {
                return BassAsio.IsStarted;
            }
        }

        public override bool IsPaused
        {
            get
            {
                return BassAsio.ChannelIsActive(false, PRIMARY_CHANNEL) == AsioChannelActive.Paused;
            }
        }

        public override bool IsStopped
        {
            get
            {
                return !BassAsio.IsStarted;
            }
        }

        public override void Play()
        {
            if (BassAsio.IsStarted)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Starting ASIO.");
            try
            {
                BassUtils.OK(this.StartAsio());
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        public override void Pause()
        {
            Logger.Write(this, LogLevel.Debug, "Pausing ASIO.");
            try
            {
                BassUtils.OK(BassAsio.ChannelPause(false, PRIMARY_CHANNEL));
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        public override void Resume()
        {
            Logger.Write(this, LogLevel.Debug, "Resuming ASIO.");
            try
            {
                BassUtils.OK(BassAsio.ChannelReset(false, PRIMARY_CHANNEL, AsioChannelResetFlags.Pause));
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        public override void Stop()
        {
            if (!BassAsio.IsStarted)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Stopping ASIO.");
            try
            {
                BassUtils.OK(BassAsio.Stop());
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }
    }
}