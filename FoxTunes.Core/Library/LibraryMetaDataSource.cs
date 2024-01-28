﻿using FoxTunes.Interfaces;
using System.Collections.ObjectModel;

namespace FoxTunes
{
    public class LibraryMetaDataSource : BaseComponent, IMetaDataSource
    {
        public LibraryMetaDataSource(LibraryItem libraryItem)
        {
            this.MetaDatas = new ObservableCollection<MetaDataItem>(libraryItem.MetaDatas);
            this.Properties = new ObservableCollection<PropertyItem>(libraryItem.Properties);
            this.Images = new ObservableCollection<ImageItem>(libraryItem.Images);
        }

        public ObservableCollection<MetaDataItem> MetaDatas { get; private set; }

        public ObservableCollection<PropertyItem> Properties { get; private set; }

        public ObservableCollection<ImageItem> Images { get; private set; }
    }
}
