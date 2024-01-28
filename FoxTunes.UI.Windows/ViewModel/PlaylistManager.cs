﻿using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class PlaylistManager : ViewModelBase
    {
        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private DoubleConfigurationElement _ScalingFactor { get; set; }

        public DoubleConfigurationElement ScalingFactor
        {
            get
            {
                return this._ScalingFactor;
            }
            set
            {
                this._ScalingFactor = value;
                this.OnScalingFactorChanged();
            }
        }

        protected virtual void OnScalingFactorChanged()
        {
            if (this.ScalingFactorChanged != null)
            {
                this.ScalingFactorChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ScalingFactor");
        }

        public event EventHandler ScalingFactorChanged;

        private CollectionManager<Playlist> _Playlists { get; set; }

        public CollectionManager<Playlist> Playlists
        {
            get
            {
                return this._Playlists;
            }
            set
            {
                this._Playlists = value;
                this.OnPlaylistsChanged();
            }
        }

        protected virtual void OnPlaylistsChanged()
        {
            this.OnPropertyChanged("Playlists");
        }

        public override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.DatabaseFactory = this.Core.Factories.Database;
            this.SignalEmitter = this.Core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = this.Core.Components.Configuration;
            this.ScalingFactor = this.Configuration.GetElement<DoubleConfigurationElement>(
              WindowsUserInterfaceConfiguration.SECTION,
              WindowsUserInterfaceConfiguration.UI_SCALING_ELEMENT
            );
            this.Playlists = new CollectionManager<Playlist>()
            {
                ItemFactory = () => new Playlist()
                {
                    Name = Playlist.GetName(this.Playlists.ItemsSource),
                    Enabled = true
                },
                ExchangeHandler = (item1, item2) =>
                {
                    var temp = item1.Sequence;
                    item1.Sequence = item2.Sequence;
                    item2.Sequence = temp;
                }
            };
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual async void OnActiveChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() => this.OnIsSavingChanged()).ConfigureAwait(false);
        }

        private Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    var playlists = signal.State as IEnumerable<Playlist>;
                    if (playlists != null && playlists.Any())
                    {

                    }
                    else
                    {
                        return this.Refresh();
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                using (var database = this.DatabaseFactory.Create())
                {
                    using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                    {
                        this.Playlists.ItemsSource = new ObservableCollection<Playlist>(
                            database.Set<Playlist>(transaction)
                        );
                    }
                }
            });
        }

        public bool PlaylistManagerVisible
        {
            get
            {
                return Windows.IsPlaylistManagerWindowCreated;
            }
            set
            {
                if (value)
                {
                    Windows.PlaylistManagerWindow.DataContext = this.Core;
                    Windows.PlaylistManagerWindow.Show();
                }
                else if (Windows.IsPlaylistManagerWindowCreated)
                {
                    Windows.PlaylistManagerWindow.Close();
                }
            }
        }

        protected virtual void OnPlaylistManagerVisibleChanged()
        {
            if (this.PlaylistManagerVisibleChanged != null)
            {
                this.PlaylistManagerVisibleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("PlaylistManagerVisible");
        }

        public event EventHandler PlaylistManagerVisibleChanged;

        public bool IsSaving
        {
            get
            {
                return global::FoxTunes.BackgroundTask.Active
                    .OfType<PlaylistTaskBase>()
                    .Any();
            }
        }

        protected virtual void OnIsSavingChanged()
        {
            if (this.IsSavingChanged != null)
            {
                this.IsSavingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSaving");
        }

        public event EventHandler IsSavingChanged;

        public ICommand SaveCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Save);
            }
        }

        public async Task Save()
        {
            var exception = default(Exception);
            try
            {
                using (var database = this.DatabaseFactory.Create())
                {
                    using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var playlists = database.Set<Playlist>(transaction);
                            foreach (var playlist in playlists.Except(this.Playlists.ItemsSource).ToArray())
                            {
                                await PlaylistTaskBase.RemovePlaylistItems(database, playlist.Id, PlaylistItemStatus.None, transaction).ConfigureAwait(false);
                                playlists.Remove(playlist);
                            }
                            playlists.AddOrUpdate(this.Playlists.ItemsSource);
                            transaction.Commit();
                        }
                    }))
                    {
                        await task.Run().ConfigureAwait(false);
                    }
                }
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated)).ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.OnError("Save", exception).ConfigureAwait(false);
            throw exception;
        }

        public ICommand CancelCommand
        {
            get
            {
                return new Command(() => this.PlaylistManagerVisible = false);
            }
        }

        protected override void OnDisposing()
        {
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaylistManager();
        }
    }
}
