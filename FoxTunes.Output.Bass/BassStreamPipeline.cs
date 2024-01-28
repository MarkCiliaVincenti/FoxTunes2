﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassStreamPipeline : BaseComponent, IBassStreamPipeline
    {
        public static readonly object SyncRoot = new object();

        public BassStreamPipeline(IBassStreamInput input, IEnumerable<IBassStreamComponent> components, IBassStreamOutput output)
        {
            this.Input = input;
            this.Components = components;
            this.Output = output;
            this.All.ForEach(component => component.Error += this.OnComponentError);
        }

        public IBassStreamInput Input { get; private set; }

        public IEnumerable<IBassStreamComponent> Components { get; private set; }

        public IBassStreamOutput Output { get; private set; }

        public IEnumerable<IBassStreamComponent> All
        {
            get
            {
                yield return this.Input;
                foreach (var component in this.Components)
                {
                    yield return component;
                }
                yield return this.Output;
            }
        }

        public IEnumerable<IBassStreamControllable> Controllable
        {
            get
            {
                return this.All.OfType<IBassStreamControllable>();
            }
        }

        public long BufferLength
        {
            get
            {
                return this.All.Sum(component => component.BufferLength);
            }
        }

        public void Connect()
        {
            var previous = (IBassStreamComponent)this.Input;
            Logger.Write(this, LogLevel.Debug, "Connecting pipeline input: \"{0}\"", this.Input.GetType().Name);
            this.Input.Connect(previous);
            foreach (var component in this.Components)
            {
                Logger.Write(this, LogLevel.Debug, "Connecting pipeline component: \"{0}\"", component.GetType().Name);
                component.Connect(previous);
                previous = component;
            }
            Logger.Write(this, LogLevel.Debug, "Connecting pipeline output: \"{0}\"", this.Output.GetType().Name);
            this.Output.Connect(previous);
        }

        public void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing pipeline buffer.");
            this.All.ForEach(component => component.ClearBuffer());
        }

        public void Play()
        {
            this.Controllable.ForEach(component => component.Play());
        }

        public void Pause()
        {
            this.Controllable.Reverse().ForEach(component => component.Pause());
        }

        public void Resume()
        {
            this.Controllable.ForEach(component => component.Resume());
        }

        public void Stop()
        {
            this.Controllable.Reverse().ForEach(component => component.Stop());
        }

        protected virtual Task OnComponentError(object sender, ComponentErrorEventArgs e)
        {
            return this.OnError(e.Message, e.Exception);
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
            this.All.ForEach(component =>
            {
                component.Error -= this.OnComponentError;
                try
                {
                    component.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Error, "Pipeline component \"{0}\" could not be disposed: {1}", component.GetType().Name, e.Message);
                }
            });
            this.IsDisposed = true;
        }

        ~BassStreamPipeline()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
