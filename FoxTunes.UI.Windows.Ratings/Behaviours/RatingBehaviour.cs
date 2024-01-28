﻿using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace FoxTunes
{
    [Component("2681C239-1291-4018-ACED-4933CC395FF6", ComponentSlots.None, priority: ComponentAttribute.PRIORITY_LOW)]
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class RatingBehaviour : StandardBehaviour, IInvocableComponent, IUIPlaylistColumnProvider, IDatabaseInitializer
    {
        public const string SET_LIBRARY_RATING = "AAAE";

        public const string SET_PLAYLIST_RATING = "AAAD";

        public string Id
        {
            get
            {
                return typeof(RatingBehaviour).AssemblyQualifiedName;
            }
        }

        public string Name
        {
            get
            {
                return "Rating Stars";
            }
        }

        public string Description
        {
            get
            {
                return null;
            }
        }

        public DataTemplate CellTemplate
        {
            get
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.Rating)))
                {
                    return (DataTemplate)XamlReader.Load(stream);
                }
            }
        }

        public IEnumerable<string> MetaData
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            if (!type.HasFlag(DatabaseInitializeType.Playlist))
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                var set = database.Set<PlaylistColumn>(transaction);
                set.Add(new PlaylistColumn()
                {
                    Name = "Rating",
                    Type = PlaylistColumnType.Plugin,
                    Sequence = 13,
                    Plugin = this.Id,
                    Enabled = false
                });
                transaction.Commit();
            }
        }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public RatingManager RatingManager { get; private set; }

        public IMetaDataBrowser MetaDataBrowser { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Popularimeter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.RatingManager = ComponentRegistry.Instance.GetComponent<RatingManager>();
            this.MetaDataBrowser = core.Components.MetaDataBrowser;
            this.Configuration = core.Components.Configuration;
            this.Popularimeter = this.Configuration.GetElement<BooleanConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.READ_POPULARIMETER_TAGS
           );
            base.InitializeComponent(core);
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Popularimeter.Value)
                {
                    if (this.LibraryManager.SelectedItem != null)
                    {
                        var invocationComponents = new Dictionary<byte, InvocationComponent>();
                        for (var a = 0; a <= 5; a++)
                        {
                            var invocationComponent = new InvocationComponent(
                                InvocationComponent.CATEGORY_LIBRARY,
                                SET_LIBRARY_RATING,
                                a == 0 ? "None" : string.Format("{0} Stars", a),
                                path: "Set Rating",
                                attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                            );
                            invocationComponents.Add((byte)a, invocationComponent);
                            yield return invocationComponent;
                        }
                        //Don't block the menu from opening while we fetch ratings.
                        this.Dispatch(() => this.GetRating(this.LibraryManager.SelectedItem, invocationComponents));
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        var invocationComponents = new Dictionary<byte, InvocationComponent>();
                        for (var a = 0; a <= 5; a++)
                        {
                            var invocationComponent = new InvocationComponent(
                                InvocationComponent.CATEGORY_PLAYLIST,
                                SET_PLAYLIST_RATING,
                                a == 0 ? "None" : string.Format("{0} Stars", a),
                                path: "Set Rating",
                                attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                            );
                            invocationComponents.Add((byte)a, invocationComponent);
                            yield return invocationComponent;
                        }
                        //Don't block the menu from opening while we fetch ratings.
                        this.Dispatch(() => this.GetRating(this.PlaylistManager.SelectedItems, invocationComponents));
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case SET_LIBRARY_RATING:
                    return this.SetLibraryRating(component.Name);
                case SET_PLAYLIST_RATING:
                    return this.SetPlaylistRating(component.Name);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task GetRating(LibraryHierarchyNode libraryHierarchyNode, Dictionary<byte, InvocationComponent> invocationComponents)
        {
            Logger.Write(this, LogLevel.Debug, "Determining rating for library hierarchy node: {0}", libraryHierarchyNode.Id);
            var rating = default(byte);
            var ratings = await this.MetaDataBrowser.GetMetaDatasAsync(libraryHierarchyNode, MetaDataItemType.Tag, CommonMetaData.Rating).ConfigureAwait(false);
            switch (ratings.Length)
            {
                case 0:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have no rating.", libraryHierarchyNode.Id);
                    rating = 0;
                    break;
                case 1:
                    if (!byte.TryParse(ratings[0].Value, out rating))
                    {
                        Logger.Write(this, LogLevel.Warn, "Library hierarchy node {0} tracks have rating \"{1}\" which is in an unknown format.", libraryHierarchyNode.Id, rating);
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have rating {1}.", libraryHierarchyNode.Id, rating);
                    break;
                default:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have multiple ratings.", libraryHierarchyNode.Id);
                    return;
            }
            foreach (var key in invocationComponents.Keys)
            {
                var invocationComponent = invocationComponents[key];
                if (key == rating)
                {
                    invocationComponent.Attributes = (byte)(invocationComponent.Attributes | InvocationComponent.ATTRIBUTE_SELECTED);
                }
            }
        }

        protected virtual async Task GetRating(IEnumerable<PlaylistItem> playlistItems, Dictionary<byte, InvocationComponent> invocationComponents)
        {
            if (playlistItems.Count() > ListViewExtensions.MAX_SELECTED_ITEMS)
            {
                //This would result in too many parameters.
                return;
            }
            var rating = default(byte);
            var ratings = await this.MetaDataBrowser.GetMetaDatasAsync(playlistItems, MetaDataItemType.Tag, CommonMetaData.Rating).ConfigureAwait(false);
            switch (ratings.Length)
            {
                case 0:
                    rating = 0;
                    break;
                case 1:
                    if (!byte.TryParse(ratings[0].Value, out rating))
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
            foreach (var key in invocationComponents.Keys)
            {
                var invocationComponent = invocationComponents[key];
                if (key == rating)
                {
                    invocationComponent.Attributes = (byte)(invocationComponent.Attributes | InvocationComponent.ATTRIBUTE_SELECTED);
                }
            }
        }

        protected virtual Task SetLibraryRating(string name)
        {
            var rating = default(byte);
            if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
            {
                rating = 0;
            }
            else if (string.IsNullOrEmpty(name) || !byte.TryParse(name.Split(' ').FirstOrDefault(), out rating))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.RatingManager.SetRating(this.LibraryManager.SelectedItem, rating);
        }

        protected virtual Task SetPlaylistRating(string name)
        {
            var rating = default(byte);
            if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
            {
                rating = 0;
            }
            else if (string.IsNullOrEmpty(name) || !byte.TryParse(name.Split(' ').FirstOrDefault(), out rating))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.RatingManager.SetRating(this.PlaylistManager.SelectedItems, rating);
        }
    }
}