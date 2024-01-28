﻿using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Artwork.xaml
    /// </summary>
    [UIComponent("66C8A9E7-0891-48DD-8086-E40F72D4D030", UIComponentSlots.NONE, "Artwork")]
    public partial class Artwork : UIComponentBase
    {
        public static readonly IArtworkProvider ArtworkProvider = ComponentRegistry.Instance.GetComponent<IArtworkProvider>();

        public static readonly IPlaybackManager PlaybackManager = ComponentRegistry.Instance.GetComponent<IPlaybackManager>();

        public static readonly ThemeLoader ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();

        public Artwork()
        {
            this.InitializeComponent();
            if (PlaybackManager != null)
            {
                PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            }
            if (ThemeLoader != null)
            {
                ThemeLoader.ThemeChanged += this.OnThemeChanged;
            }
            var task = this.Refresh();
        }

        protected virtual void OnCurrentStreamChanged(object sender, AsyncEventArgs e)
        {
            var task = this.Refresh();
        }

        protected virtual void OnThemeChanged(object sender, AsyncEventArgs e)
        {
            var task = this.Refresh();
        }

        public async Task Refresh()
        {
            var metaDataItem = default(MetaDataItem);
            var outputStream = PlaybackManager.CurrentStream;
            if (outputStream != null)
            {
                metaDataItem = await ArtworkProvider.Find(outputStream.PlaylistItem, ArtworkType.FrontCover);
                if (metaDataItem == null)
                {
                    metaDataItem = await ArtworkProvider.Find(outputStream.PlaylistItem.FileName, ArtworkType.FrontCover);
                }
            }
            if (metaDataItem == null || !File.Exists(metaDataItem.Value))
            {
                await Windows.Invoke(() =>
                {
                    using (var stream = ThemeLoader.Theme.ArtworkPlaceholder)
                    {
                        this.Background = new ImageBrush(ImageLoader.Load(stream, 0, 0))
                        {
                            Stretch = Stretch.Uniform
                        };
                    }
                    this.IsComponentEnabled = false;
                });
            }
            else
            {
                await Windows.Invoke(() =>
                {
                    this.Background = new ImageBrush(ImageLoader.Load(metaDataItem.Value, 0, 0))
                    {
                        Stretch = Stretch.Uniform
                    };
                    this.IsComponentEnabled = true;
                });
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            if (this.Parent != null)
            {
                var row = default(RowDefinition);
                var grid = this.Parent.FindAncestor<Grid>();
                if (grid != null)
                {
                    var index = Grid.GetRow(this);
                    if (index < grid.RowDefinitions.Count)
                    {
                        row = grid.RowDefinitions[index];
                        BindingHelper.AddHandler(row, RowDefinition.HeightProperty, typeof(RowDefinition), (sender, e) =>
                        {
                            this.UpdateLayoutSource(row);
                        });
                    }
                }
                this.UpdateLayoutSource(row);
            }
            base.OnVisualParentChanged(oldParent);
        }

        protected virtual void UpdateLayoutSource(RowDefinition row = null)
        {
            if (row == null || row.Height.IsAuto || row.Height.IsStar)
            {
                this.SizeChanged += this.OnSizeChanged;
            }
            else
            {
                BindingOperations.ClearBinding(this, WidthProperty);
                BindingOperations.ClearBinding(this, HeightProperty);
            }
        }

        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            BindingOperations.ClearBinding(this, WidthProperty);
            BindingOperations.ClearBinding(this, HeightProperty);
            if (this.ActualWidth > 0)
            {
                BindingOperations.SetBinding(this, HeightProperty, new Binding("ActualWidth")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                });
            }
            else if (this.ActualHeight > 0)
            {
                BindingOperations.SetBinding(this, WidthProperty, new Binding("ActualHeight")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                });
            }
            else
            {
                return;
            }
            this.SizeChanged -= this.OnSizeChanged;
        }
    }
}
