﻿using FoxTunes.Interfaces;
using ManagedBass.Gapless;
using System;

namespace FoxTunes
{
    public class BassGaplessStreamInputBehaviour : StandardBehaviour, IDisposable
    {
        public IBassOutput Output { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

        new public bool IsInitialized { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output as IBassOutput;
            this.Output.Init += this.OnInit;
            this.Output.Free += this.OnFree;
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline += this.OnCreatingPipeline;
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnInit(object sender, EventArgs e)
        {
            BassUtils.OK(BassGapless.Init());
            this.IsInitialized = true;
            Logger.Write(this, LogLevel.Debug, "BASS GAPLESS Initialized.");
        }

        protected virtual void OnFree(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Releasing BASS GAPLESS.");
            BassGapless.Free();
            this.IsInitialized = false;
        }

        protected virtual void OnCreatingPipeline(object sender, CreatingPipelineEventArgs e)
        {
            e.Input = new BassGaplessStreamInput(this, e.Stream);
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.Output != null)
            {
                this.Output.Init -= this.OnInit;
                this.Output.Free -= this.OnFree;
            }
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline -= this.OnCreatingPipeline;
            }
        }

        ~BassGaplessStreamInputBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            this.Dispose(true);
        }
    }

    public delegate void BassGaplessEventHandler(object sender, BassGaplessEventArgs e);
}