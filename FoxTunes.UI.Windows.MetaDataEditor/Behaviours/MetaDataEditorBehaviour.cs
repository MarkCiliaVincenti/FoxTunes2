﻿using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class MetaDataEditorBehaviour : StandardBehaviour, IInvocableComponent
    {
        public const string EDIT_METADATA = "LLLL";

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement MetaData { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.Configuration = core.Components.Configuration;
            this.MetaData = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.ENABLE_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.MetaData.Value)
                {
                    if (this.LibraryManager.SelectedItem != null)
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, EDIT_METADATA, "Tag");
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, EDIT_METADATA, "Tag");
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case EDIT_METADATA:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.EditLibrary();
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.EditPlaylist();
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task EditLibrary()
        {
            if (this.LibraryManager == null || this.LibraryManager.SelectedItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            //TODO: Warning: Buffering a potentially large sequence. It might be better to run the query multiple times.
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(
                this.LibraryManager.SelectedItem,
                true
            ).ToArray();
            if (!libraryItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Edit(libraryItems);
        }

        public Task EditPlaylist()
        {
            if (this.PlaylistManager == null || this.PlaylistManager.SelectedItems == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            //TODO: If the playlist contains duplicate tracks, will all be refreshed properly?
            var playlistItems = this.PlaylistManager.SelectedItems
                .GroupBy(playlistItem => playlistItem.FileName)
                .Select(group => group.First())
                .ToArray();
            if (!playlistItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Edit(playlistItems);
        }

        public Task Edit(IFileData[] fileDatas)
        {
            return Windows.Invoke(() =>
            {
                var window = new MetaDataEditorWindow()
                {
                    DataContext = this.Core,
                    ShowActivated = true,
                    Owner = Windows.ActiveWindow,
                };
                window.Show(fileDatas);
            });
        }
    }
}
