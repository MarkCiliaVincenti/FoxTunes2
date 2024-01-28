﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class MetaDataEditor : ViewModelBase
    {
        private static readonly string[] TAGS = new[]
        {
            CommonMetaData.Album,
            CommonMetaData.Artist,
            CommonMetaData.Composer,
            CommonMetaData.Conductor,
            CommonMetaData.Disc,
            CommonMetaData.DiscCount,
            CommonMetaData.Genre,
            CommonMetaData.Performer,
            CommonMetaData.Title,
            CommonMetaData.Track,
            CommonMetaData.TrackCount,
            CommonMetaData.Year
        };

        private static readonly string[] IMAGES = new[]
        {
            CommonImageTypes.FrontCover
        };

        public MetaDataEditor()
        {
            this.Tags = new ObservableCollection<MetaDataEntry>();
            this.Images = new ObservableCollection<MetaDataEntry>();
        }

        public ObservableCollection<MetaDataEntry> Tags { get; private set; }

        public ObservableCollection<MetaDataEntry> Images { get; private set; }

        private int _Count { get; set; }

        public int Count
        {
            get
            {
                return this._Count;
            }
            set
            {
                this._Count = value;
                this.OnCountChanged();
            }
        }

        protected virtual void OnCountChanged()
        {
            this.OnStatusMessageChanged();
            if (this.CountChanged != null)
            {
                this.CountChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Count");
        }

        public event EventHandler CountChanged;

        private bool _IsSaving { get; set; }

        public bool IsSaving
        {
            get
            {
                return this._IsSaving;
            }
            set
            {
                this._IsSaving = value;
                this.OnIsSavingChanged();
            }
        }

        protected virtual void OnIsSavingChanged()
        {
            this.OnStatusMessageChanged();
            if (this.IsSavingChanged != null)
            {
                this.IsSavingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSaving");
        }

        public event EventHandler IsSavingChanged;

        public string StatusMessage
        {
            get
            {
                if (this.IsSaving)
                {
                    return string.Format("Updating {0} tracks", this.Count);
                }
                else
                {
                    return string.Format("Editing {0} tracks", this.Count);
                }
            }
        }

        protected virtual void OnStatusMessageChanged()
        {
            if (this.StatusMessageChanged != null)
            {
                this.StatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StatusMessage");
        }

        public event EventHandler StatusMessageChanged;

        public ILibraryManager LibraryManager { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public IHierarchyManager HierarchyManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.MetaDataManager = core.Managers.MetaData;
            this.HierarchyManager = core.Managers.Hierarchy;
            base.InitializeComponent(core);
        }

        public void Edit(IFileData[] fileDatas)
        {
            this.SetItems(
                this.GetItems(fileDatas),
                fileDatas.Length
            );
        }

        protected IDictionary<MetaDataItemType, IEnumerable<MetaDataEntry>> GetItems(IEnumerable<IFileData> sources)
        {
            var result = new Dictionary<MetaDataItemType, IEnumerable<MetaDataEntry>>();
            result[MetaDataItemType.Tag] = TAGS.Select(name => new MetaDataEntry(name, MetaDataItemType.Tag)).ToArray();
            result[MetaDataItemType.Image] = IMAGES.Select(name => new MetaDataEntry(name, MetaDataItemType.Image)).ToArray();
            foreach (var key in result.Keys)
            {
                foreach (var metaDataEntry in result[key])
                {
                    metaDataEntry.SetSources(sources);
                }
            }
            foreach (var key in result.Keys)
            {
                foreach (var metaDataEntry in result[key])
                {
                    //TODO: Can't access this.Core on another thread.
                    metaDataEntry.InitializeComponent(null);
                }
            }
            return result;
        }

        protected virtual void SetItems(IDictionary<MetaDataItemType, IEnumerable<MetaDataEntry>> items, int count)
        {
            this.Tags.Clear();
            this.Tags.AddRange(items[MetaDataItemType.Tag]);
            this.Images.Clear();
            this.Images.AddRange(items[MetaDataItemType.Image]);
            this.Count = count;
        }

        public ICommand SaveCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Save, () => !this.IsSaving);
            }
        }

        public async Task Save()
        {
            var names = new HashSet<string>();
            var sources = new HashSet<IFileData>();
            if (this.Tags != null)
            {
                foreach (var tag in this.Tags)
                {
                    if (tag.HasChanges)
                    {
                        sources.AddRange(tag.Save());
                        names.Add(tag.Name);
                    }
                }
            }
            if (this.Images != null)
            {
                foreach (var image in this.Images)
                {
                    if (image.HasChanges)
                    {
                        sources.AddRange(image.Save());
                        names.Add(image.Name);
                    }
                }
            }
            var libraryItems = sources.OfType<LibraryItem>().ToArray();
            var playlistItems = sources.OfType<PlaylistItem>().ToArray();
            await Windows.Invoke(() => this.IsSaving = true).ConfigureAwait(false);
            try
            {
                if (libraryItems.Any())
                {
                    await this.SaveLibrary(libraryItems, names).ConfigureAwait(false);
                }
                if (playlistItems.Any())
                {
                    await this.SavePlaylist(playlistItems, names).ConfigureAwait(false);
                }
            }
            finally
            {
                await Windows.Invoke(() => this.IsSaving = false).ConfigureAwait(false);
            }
        }

        protected virtual async Task SaveLibrary(IEnumerable<LibraryItem> libraryItems, IEnumerable<string> names)
        {
            await this.MetaDataManager.Save(libraryItems, true, true, names.ToArray()).ConfigureAwait(false);
            await this.HierarchyManager.Clear(LibraryItemStatus.Import, false).ConfigureAwait(false);
            await this.HierarchyManager.Build(LibraryItemStatus.Import).ConfigureAwait(false);
            await this.LibraryManager.SetStatus(libraryItems, LibraryItemStatus.None).ConfigureAwait(false);
        }

        protected virtual async Task SavePlaylist(IEnumerable<PlaylistItem> playlistItems, IEnumerable<string> names)
        {
            await this.MetaDataManager.Save(playlistItems, true, true, names.ToArray()).ConfigureAwait(false);
            if (playlistItems.Any(playlistItem => playlistItem.LibraryItem_Id.HasValue))
            {
                await this.HierarchyManager.Clear(LibraryItemStatus.Import, false).ConfigureAwait(false);
                await this.HierarchyManager.Build(LibraryItemStatus.Import).ConfigureAwait(false);
                await this.LibraryManager.SetStatus(LibraryItemStatus.None).ConfigureAwait(false);
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MetaDataEditor();
        }
    }
}
