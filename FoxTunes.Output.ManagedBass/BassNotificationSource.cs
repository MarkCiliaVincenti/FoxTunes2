﻿using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Timers;

namespace FoxTunes
{
    public class BassNotificationSource : BaseComponent, IDisposable
    {
        const int STOPPING_THRESHOLD = 5;

        public BassNotificationSource(BassOutputStream outputStream)
        {
            this.OutputStream = outputStream;
        }

        public BassOutputStream OutputStream { get; private set; }

        public int Interval { get; set; }

        private Timer Timer { get; set; }

        public long EndingPosition
        {
            get
            {
                return this.OutputStream.Length - Bass.ChannelSeconds2Bytes(this.OutputStream.ChannelHandle, STOPPING_THRESHOLD);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Timer = new Timer();
            this.Timer.Interval = this.Interval;
            this.Timer.Elapsed += this.Timer_Elapsed;
            this.Timer.Start();
            Logger.Write(this, LogLevel.Debug, "Creating \"Ending\" channel sync {0} seconds from the end for channel: {1}", STOPPING_THRESHOLD, this.OutputStream.ChannelHandle);
            BassUtils.OK(Bass.ChannelSetSync(
                this.OutputStream.ChannelHandle,
                SyncFlags.Position,
                this.EndingPosition,
                this.OnEnding
            ));
            Logger.Write(this, LogLevel.Debug, "Creating \"End\" channel sync for channel: {0}", this.OutputStream.ChannelHandle);
            BassUtils.OK(Bass.ChannelSetSync(
                this.OutputStream.ChannelHandle,
                SyncFlags.End,
                0,
                this.OnEnded
            ));
            base.InitializeComponent(core);
        }

        protected virtual void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.OnUpdated();
        }

        protected virtual void OnEnding(int Handle, int Channel, int Data, IntPtr User)
        {
            this.Ending();
        }

        public virtual void Ending()
        {
            Logger.Write(this, LogLevel.Debug, "Channel {0} sync point reached: \"Ending\".", this.OutputStream.ChannelHandle);
            this.OnStopping();
        }

        protected virtual void OnEnded(int Handle, int Channel, int Data, IntPtr User)
        {
            this.Ended();
        }

        public virtual void Ended()
        {
            Logger.Write(this, LogLevel.Debug, "Channel {0} sync point reached: \"Ended\".", this.OutputStream.ChannelHandle);
            this.OnStopped();
        }

        protected virtual void OnUpdated()
        {
            if (this.Updated == null)
            {
                return;
            }
            this.Updated(this, BassNotificationSourceEventArgs.Empty);
        }

        public event BassNotificationSourceEventHandler Updated = delegate { };

        protected virtual void OnStopping()
        {
            if (this.Stopping == null)
            {
                return;
            }
            this.Stopping(this, BassNotificationSourceEventArgs.Empty);
        }

        public event BassNotificationSourceEventHandler Stopping = delegate { };

        protected virtual void OnStopped()
        {
            if (this.Stopped == null)
            {
                return;
            }
            this.Stopped(this, BassNotificationSourceEventArgs.Empty);
        }

        public event BassNotificationSourceEventHandler Stopped = delegate { };

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
            this.Timer.Dispose();
        }

        ~BassNotificationSource()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            this.Dispose(true);
        }
    }

    public delegate void BassNotificationSourceEventHandler(object sender, BassNotificationSourceEventArgs e);

    public class BassNotificationSourceEventArgs : EventArgs
    {
        new public static BassNotificationSourceEventArgs Empty = new BassNotificationSourceEventArgs();
    }
}
