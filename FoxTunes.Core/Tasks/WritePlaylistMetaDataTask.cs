﻿using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class WritePlaylistMetaDataTask : BackgroundTask
    {
        const MetaDataItemType META_DATA_TYPE = MetaDataItemType.Tag | MetaDataItemType.Image;

        public const string ID = "8DB1257E-5854-4F8F-BAE3-D59A45DEE998";

        public WritePlaylistMetaDataTask(IEnumerable<PlaylistItem> playlistItems, IEnumerable<string> names, bool writeToFiles)
            : base(ID)
        {
            this.PlaylistItems = playlistItems;
            this.Names = names;
            this.WriteToFiles = writeToFiles;
            this.Errors = new Dictionary<PlaylistItem, IList<string>>();
        }

        public override bool Visible
        {
            get
            {
                return this.PlaylistItems.Count() > 1;
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        public IEnumerable<PlaylistItem> PlaylistItems { get; private set; }

        public IEnumerable<string> Names { get; private set; }

        public bool WriteToFiles { get; private set; }

        public IDictionary<PlaylistItem, IList<string>> Errors { get; private set; }

        public IDatabaseComponent Database { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public IPlaylistCache PlaylistCache { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Database = core.Factories.Database.Create();
            this.MetaDataManager = core.Managers.MetaData;
            this.PlaylistCache = core.Components.PlaylistCache;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override async Task OnStarted()
        {
            if (this.Visible)
            {
                this.Name = "Saving meta data";
                this.Position = 0;
                this.Count = this.PlaylistItems.Count();
            }
            await base.OnStarted().ConfigureAwait(false);
            //We don't need a lock for this so not waiting for OnRun().
            this.UpdatePlaylistCache();
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataUpdated, this.Names)).ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            var position = 0;
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                foreach (var playlistItem in this.PlaylistItems)
                {
                    if (this.IsCancellationRequested)
                    {
                        break;
                    }

                    if (this.Visible)
                    {
                        this.Description = Path.GetFileName(playlistItem.FileName);
                        this.Position = position;
                    }

                    if (!playlistItem.LibraryItem_Id.HasValue)
                    {
                        await this.WritePlaylistMetaData(playlistItem).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.WriteLibraryMetaData(playlistItem).ConfigureAwait(false);
                        await LibraryTaskBase.SetLibraryItemStatus(this.Database, playlistItem.LibraryItem_Id.Value, LibraryItemStatus.Import).ConfigureAwait(false);
                    }

                    if (this.WriteToFiles)
                    {
                        if (!await this.MetaDataManager.Synchronize(new[] { playlistItem }, this.Names.ToArray()).ConfigureAwait(false))
                        {
                            this.AddError(playlistItem, string.Format("Failed to write meta data to file \"{0}\". We will try again later.", playlistItem.FileName));
                        }
                    }

                    position++;
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        protected virtual void UpdatePlaylistCache()
        {
            foreach (var playlistItem in this.PlaylistItems)
            {
                var cachedPlaylistItem = default(PlaylistItem);
                if (this.PlaylistCache.TryGetItemById(playlistItem.Id, out cachedPlaylistItem))
                {
                    if (!object.ReferenceEquals(playlistItem, cachedPlaylistItem))
                    {
                        MetaDataItem.Update(playlistItem, cachedPlaylistItem);
                    }
                }
            }
        }

        private async Task WritePlaylistMetaData(PlaylistItem playlistItem)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.ClearPlaylistMetaDataItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["itemId"] = playlistItem.Id;
                            parameters["type"] = META_DATA_TYPE;
                            break;
                    }
                }, transaction).ConfigureAwait(false);

                using (var writer = new MetaDataWriter(this.Database, this.Database.Queries.AddPlaylistMetaDataItem, transaction))
                {
                    await writer.Write(
                        playlistItem.Id,
                        playlistItem.MetaDatas,
                        metaDataItem => META_DATA_TYPE.HasFlag(metaDataItem.Type)
                    ).ConfigureAwait(false);
                }

                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        private async Task WriteLibraryMetaData(PlaylistItem playlistItem)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.ClearLibraryMetaDataItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["itemId"] = playlistItem.LibraryItem_Id.Value;
                            parameters["type"] = META_DATA_TYPE;
                            break;
                    }
                }, transaction).ConfigureAwait(false);

                using (var writer = new MetaDataWriter(this.Database, this.Database.Queries.AddLibraryMetaDataItem, transaction))
                {
                    await writer.Write(
                        playlistItem.LibraryItem_Id.Value,
                        playlistItem.MetaDatas,
                        metaDataItem => META_DATA_TYPE.HasFlag(metaDataItem.Type)
                    ).ConfigureAwait(false);
                }

                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        protected virtual void AddError(PlaylistItem playlistItem, string message)
        {
            var errors = default(IList<string>);
            if (!this.Errors.TryGetValue(playlistItem, out errors))
            {
                errors = new List<string>();
                this.Errors.Add(playlistItem, errors);
            }
            errors.Add(message);
        }

        protected override void OnDisposing()
        {
            if (this.Database != null)
            {
                this.Database.Dispose();
            }
            base.OnDisposing();
        }
    }
}
