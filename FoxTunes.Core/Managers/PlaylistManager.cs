﻿using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace FoxTunes.Managers
{
    public class PlaylistManager : StandardManager, IPlaylistManager
    {
        public const string CLEAR_PLAYLIST = "ZZZZ";

        private volatile bool IsNavigating = false;

        public ICore Core { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        private bool _CanNavigate { get; set; }

        public bool CanNavigate
        {
            get
            {
                return this._CanNavigate;
            }
            set
            {
                this._CanNavigate = value;
                this.OnCanNavigateChanged();
            }
        }

        protected virtual void OnCanNavigateChanged()
        {
            if (this.CanNavigateChanged != null)
            {
                this.CanNavigateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CanNavigate");
        }

        public event EventHandler CanNavigateChanged = delegate { };

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.DatabaseFactory = core.Factories.Database;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.PlaybackManager_CurrentStreamChanged;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            //TODO: Bad .Wait().
            this.Refresh().Wait();
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    return this.Refresh();
            }
            return Task.CompletedTask;
        }

        public async Task<bool> HasItems()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.ExecuteScalarAsync<bool>(database.QueryFactory.Build().With(query1 =>
                    {
                        query1.Output.AddCase(
                            query1.Output.CreateCaseCondition(
                                query1.Output.CreateFunction(
                                    QueryFunction.Exists,
                                    query1.Output.CreateSubQuery(
                                        database.QueryFactory.Build().With(query2 =>
                                        {
                                            query2.Output.AddOperator(QueryOperator.Star);
                                            query2.Source.AddTable(database.Tables.PlaylistItem);
                                        })
                                    )
                                ),
                                query1.Output.CreateConstant(1)
                            ),
                            query1.Output.CreateCaseCondition(
                                query1.Output.CreateConstant(0)
                            )
                        );
                    }), transaction);
                }
            }
        }

        public async Task Refresh()
        {
            Logger.Write(this, LogLevel.Debug, "Refresh was requested, determining whether navigation is possible.");
            this.CanNavigate = this.DatabaseFactory != null && await this.HasItems();
            if (this.CanNavigate)
            {
                Logger.Write(this, LogLevel.Debug, "Navigation is possible.");
                if (this.CurrentItem != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshing current item.");
                    using (var database = this.DatabaseFactory.Create())
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var set = database.Set<PlaylistItem>(transaction);
                            if ((this.CurrentItem = await set.FindAsync(this.CurrentItem.Id)) == null)
                            {
                                Logger.Write(this, LogLevel.Warn, "Failed to refresh current item.");
                            }
                        }
                    }
                }
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Navigation is not possible, playlist is empty.");
            }
        }

        protected virtual void PlaybackManager_CurrentStreamChanged(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Playback manager output stream changed, updating current playlist item.");
            if (this.PlaybackManager.CurrentStream == null)
            {
                this.CurrentItem = null;
                Logger.Write(this, LogLevel.Debug, "Playback manager output stream is empty. Cleared current playlist item.");
            }
            else if (this.PlaybackManager.CurrentStream.PlaylistItem != this.CurrentItem)
            {
                this.CurrentItem = this.PlaybackManager.CurrentStream.PlaylistItem;
                Logger.Write(this, LogLevel.Debug, "Updated current playlist item: {0} => {1}", this.CurrentItem.Id, this.CurrentItem.FileName);
            }
        }

        public async Task Add(IEnumerable<string> paths, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding paths to playlist.");
            var index = await this.GetInsertIndex();
            await this.Insert(index, paths, clear);
        }

        public async Task Insert(int index, IEnumerable<string> paths, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting paths into playlist at index: {0}", index);
            using (var task = new AddPathsToPlaylistTask(index, paths, clear))
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run();
            }
        }

        public async Task Add(LibraryHierarchyNode libraryHierarchyNode, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding library node to playlist.");
            var index = await this.GetInsertIndex();
            await this.Insert(index, libraryHierarchyNode, clear);
        }

        public async Task Insert(int index, LibraryHierarchyNode libraryHierarchyNode, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting library node into playlist at index: {0}", index);
            using (var task = new AddLibraryHierarchyNodeToPlaylistTask(index, libraryHierarchyNode, clear))
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run();
            }
        }

        public async Task Move(IEnumerable<PlaylistItem> playlistItems)
        {
            Logger.Write(this, LogLevel.Debug, "Re-ordering playlist items.");
            var index = await this.GetInsertIndex();
            await this.Move(index, playlistItems);
        }

        public async Task Move(int index, IEnumerable<PlaylistItem> playlistItems)
        {
            Logger.Write(this, LogLevel.Debug, "Re-ordering playlist items at index: {0}", index);
            using (var task = new MovePlaylistItemsTask(index, playlistItems))
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run();
            }
        }

        public async Task Remove(IEnumerable<PlaylistItem> playlistItems)
        {
            using (var task = new RemoveItemsFromPlaylistTask(playlistItems))
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run();
            }
        }

        public async Task Crop(IEnumerable<PlaylistItem> playlistItems)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var queryable = database.AsQueryable<PlaylistItem>(transaction);
                    var query = queryable.Except(playlistItems);
                    //TODO: Bad .ToArray()
                    using (var task = new RemoveItemsFromPlaylistTask(query.ToArray()))
                    {
                        task.InitializeComponent(this.Core);
                        this.OnBackgroundTask(task);
                        await task.Run();
                    }
                }
            }
        }

        public async Task<int> GetInsertIndex()
        {
            var playlistItem = await this.GetLastPlaylistItem();
            if (playlistItem == null)
            {
                return 0;
            }
            else
            {
                return playlistItem.Sequence + 1;
            }
        }

        public async Task<PlaylistItem> GetNext()
        {
            var playlistItem = default(PlaylistItem);
            if (this.CurrentItem == null)
            {
                Logger.Write(this, LogLevel.Debug, "Current playlist item is empty, assuming first item.");
                playlistItem = await this.GetFirstPlaylistItem();
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Current playlist item is sequence: {0}", this.CurrentItem.Sequence);
                playlistItem = await this.GetNextPlaylistItem(this.CurrentItem.Sequence);
                if (playlistItem == null)
                {
                    Logger.Write(this, LogLevel.Debug, "Sequence was too large, wrapping around to first item.");
                    playlistItem = await this.GetFirstPlaylistItem();
                }
            }
            if (playlistItem == null)
            {
                Logger.Write(this, LogLevel.Debug, "Playlist was empty.");
            }
            return playlistItem;
        }

        public async Task Next()
        {
            Logger.Write(this, LogLevel.Debug, "Navigating to next playlist item.");
            if (this.IsNavigating)
            {
                Logger.Write(this, LogLevel.Debug, "Already navigating, ignoring request.");
                return;
            }
            try
            {
                this.IsNavigating = true;
                var playlistItem = await this.GetNext();
                if (playlistItem == null)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Playing playlist item: {0} => {1}", playlistItem.Id, playlistItem.FileName);
                await this.Play(playlistItem);
            }
            finally
            {
                this.IsNavigating = false;
            }
        }

        public async Task<PlaylistItem> GetPrevious()
        {
            var playlistItem = default(PlaylistItem);
            if (this.CurrentItem == null)
            {
                Logger.Write(this, LogLevel.Debug, "Current playlist item is empty, assuming last item.");
                playlistItem = await this.GetLastPlaylistItem();
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Previous playlist item is sequence: {0}", this.CurrentItem.Sequence);
                playlistItem = await this.GetPreviousPlaylistItem(this.CurrentItem.Sequence);
                if (playlistItem == null)
                {
                    Logger.Write(this, LogLevel.Debug, "Sequence was too small, wrapping around to last item.");
                    playlistItem = await this.GetLastPlaylistItem();
                }
            }
            if (playlistItem == null)
            {
                Logger.Write(this, LogLevel.Debug, "Playlist was empty.");
            }
            return playlistItem;
        }

        public async Task Previous()
        {
            Logger.Write(this, LogLevel.Debug, "Navigating to previous playlist item.");
            if (this.IsNavigating)
            {
                Logger.Write(this, LogLevel.Debug, "Already navigating, ignoring request.");
                return;
            }
            try
            {
                this.IsNavigating = true;
                var playlistItem = await this.GetPrevious();
                Logger.Write(this, LogLevel.Debug, "Playing playlist item: {0} => {1}", playlistItem.Id, playlistItem.FileName);
                await this.Play(playlistItem);
            }
            finally
            {
                this.IsNavigating = false;
            }
        }

        protected virtual async Task<PlaylistItem> GetPlaylistItem(int sequence)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .Where(playlistItem => playlistItem.Sequence == sequence)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        protected virtual async Task<PlaylistItem> GetPlaylistItem(string fileName)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .Where(playlistItem => playlistItem.FileName == fileName)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        protected virtual async Task<PlaylistItem> GetFirstPlaylistItem()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .OrderBy(playlistItem => playlistItem.Sequence)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        protected virtual async Task<PlaylistItem> GetLastPlaylistItem()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .OrderByDescending(playlistItem => playlistItem.Sequence)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        protected virtual async Task<PlaylistItem> GetNextPlaylistItem(int sequence)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .Where(playlistItem => playlistItem.Sequence > sequence)
                        .OrderBy(playlistItem => playlistItem.Sequence)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        protected virtual async Task<PlaylistItem> GetPreviousPlaylistItem(int sequence)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.AsQueryable<PlaylistItem>(transaction)
                        .Where(playlistItem => playlistItem.Sequence < sequence)
                        .OrderByDescending(playlistItem => playlistItem.Sequence)
                        .Take(1)
                        .WithAsyncEnumerator(enumerator => enumerator.FirstOrDefault());
                }
            }
        }

        public async Task Play(PlaylistItem playlistItem)
        {
            await this.PlaybackManager.Load(playlistItem, true);
            if (this.CurrentItem == null)
            {
                Logger.Write(this, LogLevel.Warn, "Expected current output stream to be {0} => {1} but it empty.", playlistItem.Id, playlistItem.FileName);
                return;
            }
            if (this.CurrentItem != playlistItem)
            {
                Logger.Write(this, LogLevel.Warn, "Expected current output stream to be {0} => {1} but it was {2} => {3}", playlistItem.Id, playlistItem.FileName, this.CurrentItem.Id, this.CurrentItem.FileName);
                return;
            }
            if (this.PlaybackManager.CurrentStream.PlaylistItem != playlistItem)
            {
                Logger.Write(this, LogLevel.Warn, "Expected current output stream to be {0} => {1} but it was {2} => {3}", playlistItem.Id, playlistItem.FileName, this.PlaybackManager.CurrentStream.PlaylistItem.Id, this.PlaybackManager.CurrentStream.PlaylistItem.FileName);
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Playing current output stream: {0} => {1}", playlistItem.Id, playlistItem.FileName);
            var exception = default(Exception);
            try
            {
                await this.PlaybackManager.CurrentStream.Play();
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.OnError(exception);
        }

        public async Task Play(string fileName)
        {
            var playlistItem = await this.GetPlaylistItem(fileName);
            if (playlistItem == null)
            {
                return;
            }
            await this.Play(playlistItem);
        }

        public async Task Play(int sequence)
        {
            var playlistItem = await this.GetPlaylistItem(sequence);
            if (playlistItem == null)
            {
                return;
            }
            await this.Play(playlistItem);
        }

        public async Task Clear()
        {
            using (var task = new ClearPlaylistTask())
            {
                task.InitializeComponent(this.Core);
                this.OnBackgroundTask(task);
                await task.Run();
            }
        }

        private PlaylistItem _CurrentItem { get; set; }

        public PlaylistItem CurrentItem
        {
            get
            {
                return this._CurrentItem;
            }
            private set
            {
                this._CurrentItem = value;
                this.OnCurrentItemChanged();
            }
        }

        protected virtual void OnCurrentItemChanged()
        {
            if (this.CurrentItemChanged != null)
            {
                this.CurrentItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentItem");
        }

        public event EventHandler CurrentItemChanged = delegate { };

        protected virtual void OnBackgroundTask(IBackgroundTask backgroundTask)
        {
            if (this.BackgroundTask == null)
            {
                return;
            }
            this.BackgroundTask(this, new BackgroundTaskEventArgs(backgroundTask));
        }

        public event BackgroundTaskEventHandler BackgroundTask = delegate { };

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, CLEAR_PLAYLIST, "Clear", attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case CLEAR_PLAYLIST:
                    return this.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
