﻿using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Mix;
using ManagedBass.Wasapi;
using System;
using System.Linq;
using System.Threading;

namespace FoxTunes
{
    public class BassWasapiStreamOutput : BassStreamOutput
    {
        const int CONNECT_ATTEMPTS = 5;

        const int CONNECT_ATTEMPT_INTERVAL = 400;

        const int START_ATTEMPTS = 5;

        const int START_ATTEMPT_INTERVAL = 400;

        private BassWasapiStreamOutput()
        {
            this.Flags = BassFlags.Default;
        }

        public BassWasapiStreamOutput(BassWasapiStreamOutputBehaviour behaviour, BassOutputStream stream)
            : this()
        {
            this.Behaviour = behaviour;
            this.Rate = behaviour.Output.Rate;
            this.Channels = BassWasapiDevice.Info.Outputs;
            //WASAPI requires BASS_SAMPLE_FLOAT so don't bother respecting the output's Float setting.
            this.Flags = BassFlags.Decode | BassFlags.Float;
        }

        public override string Name
        {
            get
            {
                return "WASAPI";
            }
        }

        public override string Description
        {
            get
            {
                return string.Format(
                    "{0} ({1}/{2} {3})",
                    this.Name,
                    BassUtils.DepthDescription(this.Flags),
                    BassUtils.RateDescription(this.Rate),
                    BassUtils.ChannelDescription(this.Channels)
                );
            }
        }

        public BassWasapiStreamOutputBehaviour Behaviour { get; private set; }

        public int Device
        {
            get
            {
                return BassWasapiDevice.Device;
            }
        }

        public override int Rate { get; protected set; }

        public override int Channels { get; protected set; }

        public override BassFlags Flags { get; protected set; }

        public override int ChannelHandle { get; protected set; }

        public override bool CheckFormat(int rate, int channels)
        {
            return BassWasapiDevice.Info.SupportedRates.Contains(rate) && channels <= BassWasapiDevice.Info.Outputs;
        }

        public override void Connect(IBassStreamComponent previous)
        {
            Logger.Write(this, LogLevel.Debug, "Creating BASS MIX stream with rate {0} and {1} channels.", this.Rate, this.Channels);
            this.ChannelHandle = BassMix.CreateMixerStream(this.Rate, this.Channels, this.Flags);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
            Logger.Write(this, LogLevel.Debug, "Adding stream to the mixer: {0}", previous.ChannelHandle);
            BassUtils.OK(BassMix.MixerAddChannel(this.ChannelHandle, previous.ChannelHandle, BassFlags.Default));
            BassWasapiDevice.Init(this.Rate, this.Channels);
            BassUtils.OK(BassWasapiHandler.StreamSet(this.ChannelHandle));
        }

        protected virtual bool StartWASAPI()
        {
            for (var a = 1; a <= START_ATTEMPTS; a++)
            {
                Logger.Write(this, LogLevel.Debug, "Starting WASAPI, attempt: {0}", a);
                try
                {
                    var success = BassWasapi.Start();
                    if (success)
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully started WASAPI.");
                        return true;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to start WASAPI: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                    }
                }
                catch (Exception e)
                {
                    //Nothing can be done.
                    Logger.Write(this, LogLevel.Warn, "Failed to start WASAPI: {0}", e.Message);
                }
                Thread.Sleep(START_ATTEMPT_INTERVAL);
            }
            Logger.Write(this, LogLevel.Warn, "Failed to start WASAPI after {0} attempts.", START_ATTEMPTS);
            return false;
        }

        protected virtual bool StopWASAPI(bool reset)
        {
            if (!BassWasapi.IsStarted)
            {
                Logger.Write(this, LogLevel.Debug, "WASAPI has not been started.");
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Stopping WASAPI.");
            BassUtils.OK(BassWasapi.Stop(reset));
            return true;
        }

        public override bool IsPlaying
        {
            get
            {
                return BassWasapi.IsStarted;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsPaused { get; protected set; }

        public override bool IsStopped
        {
            get
            {
                return !BassWasapi.IsStarted;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override int Latency
        {
            get
            {
                return 0;
            }
        }

        public override void Play()
        {
            if (this.IsPlaying)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Starting WASAPI.");
            try
            {
                BassUtils.OK(this.StartWASAPI());
                this.IsPaused = false;
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        public override void Pause()
        {
            if (this.IsStopped)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Pausing WASAPI.");
            try
            {
                BassUtils.OK(this.StopWASAPI(false));
                this.IsPaused = true;
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        public override void Resume()
        {
            this.Play();
        }

        public override void Stop()
        {
            if (this.IsStopped)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Stopping WASAPI.");
            try
            {
                BassUtils.OK(this.StopWASAPI(true));
                this.IsPaused = false;
            }
            catch (Exception e)
            {
                this.OnError(e);
            }
        }

        protected override void OnDisposing()
        {
            if (BassWasapi.IsStarted)
            {
                BassUtils.OK(this.StopWASAPI(true));
            }
        }
    }
}