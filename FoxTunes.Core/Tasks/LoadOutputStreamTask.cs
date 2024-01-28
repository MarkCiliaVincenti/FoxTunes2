﻿using FoxTunes.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LoadOutputStreamTask : BackgroundTask
    {
        public const string ID = "E3E23677-DE0A-4291-8416-BC4A91856037";

        public LoadOutputStreamTask(PlaylistItem playlistItem, bool immediate)
            : base(ID, immediate)
        {
            this.PlaylistItem = playlistItem; this.Immediate = immediate;
        }

        public PlaylistItem PlaylistItem { get; private set; }

        public bool Immediate { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStreamQueue OutputStreamQueue { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.OutputStreamQueue = core.Components.OutputStreamQueue;
            base.InitializeComponent(core);
        }

        protected override Task OnRun()
        {
            this.Name = "Buffering";
            this.Description = new FileInfo(this.PlaylistItem.FileName).Name;
            Logger.Write(this, LogLevel.Debug, "Loading play list item into output stream: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
            return this.OutputStreamQueue.Interlocked(async () =>
            {
                if (this.OutputStreamQueue.IsQueued(this.PlaylistItem))
                {
                    Logger.Write(this, LogLevel.Debug, "Play list item already exists in the queue:  {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                    if (this.Immediate)
                    {
                        Logger.Write(this, LogLevel.Debug, "Immediate load was requested, de-queuing: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                        this.OutputStreamQueue.Dequeue(this.PlaylistItem);
                    }
                    return;
                }
                var outputStream = await this.Output.Load(this.PlaylistItem);
                Logger.Write(this, LogLevel.Debug, "Play list item loaded into output stream: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                this.OutputStreamQueue.Enqueue(outputStream, this.Immediate);
                Logger.Write(this, LogLevel.Debug, "Output stream added to the queue: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
            });
        }
    }
}
