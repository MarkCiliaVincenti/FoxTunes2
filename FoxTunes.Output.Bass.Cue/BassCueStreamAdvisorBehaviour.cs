﻿using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassCueStreamAdvisorBehaviour : StandardBehaviour, IConfigurableComponent, IBackgroundTaskSource, IInvocableComponent, IFileActionHandler, IDisposable
    {
        public const string CUE = ".cue";

        public const string OPEN_CUE = "FFGG";

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public IBassOutput Output { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private bool _Enabled { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                Logger.Write(this, LogLevel.Debug, "Enabled = {0}", this.Enabled);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Output = core.Components.Output as IBassOutput;
            this.PlaylistManager = core.Managers.Playlist;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassCueStreamAdvisorBehaviourConfiguration.SECTION,
                BassCueStreamAdvisorBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => this.Enabled = value);
            base.InitializeComponent(core);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassCueStreamAdvisorBehaviourConfiguration.GetConfigurationSections();
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, OPEN_CUE, "Open CUE");
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case OPEN_CUE:
                    return this.OpenCue();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public bool CanHandle(string path)
        {
            if (!this.Enabled)
            {
                return false;
            }
            if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), CUE, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public async Task Handle(IEnumerable<string> paths)
        {
            var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            foreach (var path in paths)
            {
                await this.OpenCue(playlist, path).ConfigureAwait(false);
            }
        }

        public Task OpenCue()
        {
            var options = new BrowseOptions(
                "Open",
                string.Empty,
                new[]
                {
                    new BrowseFilter("Cue sheets", new[]
                    {
                        CUE
                    })
                },
                BrowseFlags.File
            );
            var result = this.FileSystemBrowser.Browse(options);
            if (!result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.OpenCue(this.PlaylistManager.SelectedPlaylist, result.Paths.FirstOrDefault());
        }

        public async Task OpenCue(Playlist playlist, string fileName)
        {
            using (var task = new AddCueToPlaylistTask(playlist, fileName))
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run().ConfigureAwait(false);
            }
        }

        protected virtual void OnBackgroundTask(IBackgroundTask backgroundTask)
        {
            if (this.BackgroundTask == null)
            {
                return;
            }
            this.BackgroundTask(this, new BackgroundTaskEventArgs(backgroundTask));
        }

        public event BackgroundTaskEventHandler BackgroundTask;

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
            //Nothing to do.
        }

        ~BassCueStreamAdvisorBehaviour()
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

        private class AddCueToPlaylistTask : PlaylistTaskBase
        {
            public AddCueToPlaylistTask(Playlist playlist, string fileName) : base(playlist)
            {
                this.FileName = fileName;
            }

            public string FileName { get; private set; }

            public IPlaylistManager PlaylistManager { get; private set; }

            public IPlaylistBrowser PlaylistBrowser { get; private set; }

            public CueSheetParser Parser { get; private set; }

            public CueSheetPlaylistItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.PlaylistManager = core.Managers.Playlist;
                this.PlaylistBrowser = core.Components.PlaylistBrowser;
                this.Parser = new CueSheetParser();
                this.Parser.InitializeComponent(core);
                this.Factory = new CueSheetPlaylistItemFactory();
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                var cueSheet = this.Parser.Parse(this.FileName);
                var playlist = this.PlaylistManager.SelectedPlaylist;
                var playlistItems = await this.Factory.Create(cueSheet).ConfigureAwait(false);
                using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    //Always append for now.
                    this.Sequence = this.PlaylistBrowser.GetInsertIndex(playlist);
                    await this.AddPlaylistItems(playlist, playlistItems).ConfigureAwait(false);
                    await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
                    await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new[] { this.Playlist })).ConfigureAwait(false);
            }

            private async Task AddPlaylistItems(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<PlaylistItem>(transaction);
                    var position = 0;
                    foreach (var playlistItem in playlistItems)
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", playlistItem.FileName);
                        playlistItem.Playlist_Id = playlist.Id;
                        playlistItem.Sequence = this.Sequence + position;
                        playlistItem.Status = PlaylistItemStatus.Import;
                        await set.AddAsync(playlistItem).ConfigureAwait(false);
                        position++;
                    }
                    this.Offset += position;
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }
        }
    }
}
