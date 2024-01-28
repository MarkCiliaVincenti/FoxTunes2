﻿using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Sox;
using System;
using System.Linq;

namespace FoxTunes
{
    public class BassResamplerStreamComponent : BassStreamComponent, IBassStreamControllable
    {
        public BassResamplerStreamComponent(BassResamplerStreamComponentBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            this.Behaviour = behaviour;
            this.Query = query;
            this.Rate = behaviour.Output.Rate;
            this.Channels = stream.Channels;
            this.Flags = BassFlags.Decode;
            if (this.Behaviour.Output.Float)
            {
                this.Flags |= BassFlags.Float;
            }
        }

        public override string Name
        {
            get
            {
                return "Resampler";
            }
        }

        public override string Description
        {
            get
            {
                return string.Format(
                    "{0} ({1}/{2} -> {1}/{3})",
                    this.Name,
                    BassUtils.DepthDescription(this.Flags),
                    MetaDataInfo.SampleRateDescription(this.InputRate),
                    MetaDataInfo.SampleRateDescription(this.Rate)
                );
            }
        }

        public BassResamplerStreamComponentBehaviour Behaviour { get; private set; }

        public IBassStreamPipelineQueryResult Query { get; private set; }

        public int InputRate { get; protected set; }

        public override int Rate { get; protected set; }

        public override int Channels { get; protected set; }

        public override BassFlags Flags { get; protected set; }

        public override int ChannelHandle { get; protected set; }

        protected int _BufferLength { get; private set; }

        public override long BufferLength
        {
            get
            {
                var length = default(int);
                BassUtils.OK(BassSox.StreamBufferLength(this.ChannelHandle, out length));
                return length;
            }
            protected set
            {
                this._BufferLength = (int)value;
                this.Configure();
            }
        }

        public override bool IsActive
        {
            get
            {
                return true;
            }
        }

        public IConfiguration Configuration { get; private set; }

        private SoxChannelQuality _Quality { get; set; }

        public SoxChannelQuality Quality
        {
            get
            {
                return this._Quality;
            }
            set
            {
                this._Quality = value;
                this.OnQualityChanged();
            }
        }

        protected virtual void OnQualityChanged()
        {
            this.Configure();
            if (this.QualityChanged != null)
            {
                this.QualityChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Quality");
        }

        public event EventHandler QualityChanged;

        private SoxChannelPhase _Phase { get; set; }

        public SoxChannelPhase Phase
        {
            get
            {
                return this._Phase;
            }
            set
            {
                this._Phase = value;
                this.OnPhaseChanged();
            }
        }

        protected virtual void OnPhaseChanged()
        {
            this.Configure();
            if (this.PhaseChanged != null)
            {
                this.PhaseChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Phase");
        }

        public event EventHandler PhaseChanged;

        private bool _SteepFilter { get; set; }

        public bool SteepFilter
        {
            get
            {
                return this._SteepFilter;
            }
            set
            {
                this._SteepFilter = value;
                this.OnSteepFilterChanged();
            }
        }

        protected virtual void OnSteepFilterChanged()
        {
            this.Configure();
            if (this.SteepFilterChanged != null)
            {
                this.SteepFilterChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SteepFilter");
        }

        public event EventHandler SteepFilterChanged;

        private bool _AllowAliasing { get; set; }

        public bool AllowAliasing
        {
            get
            {
                return this._AllowAliasing;
            }
            set
            {
                this._AllowAliasing = value;
                this.OnAllowAliasingChanged();
            }
        }

        protected virtual void OnAllowAliasingChanged()
        {
            this.Configure();
            if (this.AllowAliasingChanged != null)
            {
                this.AllowAliasingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AllowAliasing");
        }

        public event EventHandler AllowAliasingChanged;

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassResamplerStreamComponentConfiguration.QUALITY_ELEMENT
            ).ConnectValue(value => this.Quality = BassResamplerStreamComponentConfiguration.GetQuality(value));
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassResamplerStreamComponentConfiguration.PHASE_ELEMENT
            ).ConnectValue(value => this.Phase = BassResamplerStreamComponentConfiguration.GetPhase(value));
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassResamplerStreamComponentConfiguration.STEEP_FILTER_ELEMENT
            ).ConnectValue(value => this.SteepFilter = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassResamplerStreamComponentConfiguration.ALLOW_ALIASING_ELEMENT
            ).ConnectValue(value => this.AllowAliasing = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassResamplerStreamComponentConfiguration.BUFFER_LENGTH_ELEMENT
            ).ConnectValue(value => this.BufferLength = value);
            base.InitializeComponent(core);
        }

        public override void Connect(IBassStreamComponent previous)
        {
            this.InputRate = previous.Rate;
            if (this.Behaviour.Output.EnforceRate)
            {
                //Rate is enforced.
                this.Rate = this.Behaviour.Output.Rate;
            }
            else
            {
                //We already established that the output does not support the stream rate so use the closest one.
                this.Rate = this.Query.GetNearestRate(previous.Rate);
            }
            Logger.Write(this, LogLevel.Debug, "Creating BASS SOX stream with rate {0} => {1} and {2} channels.", previous.Rate, this.Rate, this.Channels);
            this.ChannelHandle = BassSox.StreamCreate(this.Rate, this.Flags, previous.ChannelHandle);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
            this.Configure();
        }

        protected virtual void Configure()
        {
            if (this.ChannelHandle == 0)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Quality), Enum.GetName(typeof(SoxChannelQuality), this.Quality));
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Quality, this.Quality));
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Phase), Enum.GetName(typeof(SoxChannelPhase), this.Phase));
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Phase, this.Phase));
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.SteepFilter), this.SteepFilter);
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.SteepFilter, this.SteepFilter));
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.AllowAliasing), this.AllowAliasing);
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.AllowAliasing, this.AllowAliasing));
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.BufferLength), this._BufferLength);
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.BufferLength, this._BufferLength));
            Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.KeepAlive), true);
            BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.KeepAlive, true));
        }

        public override void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing BASS SOX buffer: {0}", this.ChannelHandle);
            BassUtils.OK(BassSox.StreamBufferClear(this.ChannelHandle));
        }

        public bool IsBackground
        {
            get
            {
                var background = default(int);
                BassUtils.OK(BassSox.ChannelGetAttribute(this.ChannelHandle, SoxChannelAttribute.Background, out background));
                return Convert.ToBoolean(background);
            }
            set
            {
                if (this.IsBackground == value)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Background), value);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Background, value));
            }
        }

        public void PreviewPlay()
        {
            //Nothing to do.
        }

        public void PreviewPause()
        {
            //Nothing to do.
        }

        public void PreviewResume()
        {
            //Nothing to do.
        }

        public void PreviewStop()
        {
            //Nothing to do.
        }

        public void Play()
        {
            this.IsBackground = true;
        }

        public void Pause()
        {
            this.IsBackground = false;
        }

        public void Resume()
        {
            this.IsBackground = true;
        }

        public void Stop()
        {
            this.IsBackground = false;
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Freeing BASS SOX stream: {0}", this.ChannelHandle);
                BassUtils.OK(BassSox.StreamFree(this.ChannelHandle));
            }
        }

        public static bool ShouldCreateResampler(BassResamplerStreamComponentBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                //Cannot resample DSD.
                return false;
            }
            if (behaviour.Output.EnforceRate && behaviour.Output.Rate != stream.Rate)
            {
                //Rate is enforced and not equal to the stream rate.
                return true;
            }
            if (!query.OutputRates.Contains(stream.Rate))
            {
                //Output does not support the stream rate.
                return true;
            }
            //Something else.
            return false;
        }
    }
}
