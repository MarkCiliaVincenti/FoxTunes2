﻿using System;
using System.Collections.ObjectModel;

namespace FoxTunes.Interfaces
{
    public interface IPlaylist : IBaseComponent
    {
        ObservableCollection<PlaylistItem> Items { get; }

        PlaylistItem SelectedItem { get; set; }

        event EventHandler SelectedItemChanging;

        event EventHandler SelectedItemChanged;
    }
}
