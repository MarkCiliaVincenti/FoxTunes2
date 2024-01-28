﻿using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for LibraryBrowser.xaml
    /// </summary>
    [UIComponent("FB75ECEC-A89A-4DAD-BA8D-9DB43F3DE5E3", UIComponentSlots.TOP_CENTER, "Library Browser")]
    public partial class LibraryBrowser : UIComponentBase
    {
        public static readonly ArtworkGridLoader ArtworkGridLoader = ComponentRegistry.Instance.GetComponent<ArtworkGridLoader>();

        public LibraryBrowser()
        {
            this.InitializeComponent();
        }

        protected virtual void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        protected virtual void DragSourceInitialized(object sender, ListBoxExtensions.DragSourceInitializedEventArgs e)
        {
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.LibraryBrowser>("ViewModel");
            if (viewModel != null)
            {
                if (LibraryHierarchyNode.Empty.Equals(viewModel.SelectedItem))
                {
                    return;
                }
                if (!viewModel.SelectedItem.IsMetaDatasLoaded)
                {
                    viewModel.SelectedItem.LoadMetaDatas();
                }
                this.MouseCursorAdorner.DataContext = viewModel.SelectedItem;
            }
            this.MouseCursorAdorner.Show();
            try
            {
                DragDrop.DoDragDrop(
                    this,
                    e.Data,
                    DragDropEffects.Copy
                );
            }
            finally
            {
                this.MouseCursorAdorner.Hide();
            }
        }

        protected virtual void OnIsItemVisibleChanged(object sender, ListBoxExtensions.IsItemVisibleChangedEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            if (element == null)
            {
                return;
            }
            var artworkGrid = element.GetVisualChild<ArtworkGrid>();
            if (artworkGrid == null)
            {
                return;
            }
            if (e.IsItemVisible && artworkGrid.Background == null)
            {
                ArtworkGridLoader.Load(artworkGrid);
            }
            else
            {
                ArtworkGridLoader.Cancel(artworkGrid);
            }
        }
    }
}
