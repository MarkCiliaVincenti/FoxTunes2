﻿using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class PreemptNextItemBehaviour : StandardBehaviour, IDisposable
    {
        public IOutput Output { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IOutputStreamQueue OutputStreamQueue { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.Ending += this.OnEnding;
            this.OutputStreamQueue = core.Components.OutputStreamQueue;
            base.InitializeComponent(core);
        }

        protected virtual async void OnEnding(object sender, AsyncEventArgs e)
        {
            using (e.Defer())
            {
                await this.PreemptItems().ConfigureAwait(false);
            }
        }

        private async Task PreemptItems()
        {
            var playlistItem = await this.PlaylistBrowser.GetNext(false).ConfigureAwait(false);
            if (playlistItem == null)
            {
                return;
            }
            var outputStream = this.OutputStreamQueue.Peek(playlistItem);
            if (outputStream == null)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Current stream is about to end, pre-empting the next stream: {0} => {1}", outputStream.Id, outputStream.FileName);
            if (!await this.Output.Preempt(outputStream).ConfigureAwait(false))
            {
                Logger.Write(this, LogLevel.Debug, "Pre-empt failed for stream: {0} => {1}", outputStream.Id, outputStream.FileName);
            }
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
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.Ending -= this.OnEnding;
            }
        }

        ~PreemptNextItemBehaviour()
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
