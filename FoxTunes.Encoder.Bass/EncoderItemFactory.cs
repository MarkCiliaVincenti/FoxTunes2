﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class EncoderItemFactory : BaseFactory
    {
        public static readonly object SyncRoot = new object();

        public EncoderItemFactory()
        {
            this.Settings = new Dictionary<string, IBassEncoderSettings>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, IBassEncoderSettings> Settings { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public BassEncoderOutputDestination Destination { get; private set; }

        private string _BrowseFolder { get; set; }

        public bool GetBrowseFolder(string fileName, out string directoryName)
        {
            lock (SyncRoot)
            {
                if (string.IsNullOrEmpty(this._BrowseFolder))
                {
                    var options = new BrowseOptions(
                        "Save As",
                        Path.GetDirectoryName(fileName),
                        Enumerable.Empty<BrowseFilter>(),
                        BrowseFlags.Folder
                    );
                    var result = this.FileSystemBrowser.Browse(options);
                    if (!result.Success)
                    {
                        Logger.Write(this, LogLevel.Debug, "Save As folder browse dialog was cancelled.");
                        directoryName = null;
                        return false;
                    }
                    this._BrowseFolder = result.Paths.FirstOrDefault();
                    Logger.Write(this, LogLevel.Debug, "Browse folder: {0}", this._BrowseFolder);
                }
            }
            directoryName = this._BrowseFolder;
            return true;
        }

        public string SpecificFolder { get; private set; }

        public BassEncoderSettingsFactory SettingsFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            core.Components.Configuration.GetElement<SelectionConfigurationElement>(
                BassEncoderBehaviourConfiguration.SECTION,
                BassEncoderBehaviourConfiguration.DESTINATION_ELEMENT
            ).ConnectValue(value => this.Destination = BassEncoderBehaviourConfiguration.GetDestination(value));
            core.Components.Configuration.GetElement<TextConfigurationElement>(
                BassEncoderBehaviourConfiguration.SECTION,
                BassEncoderBehaviourConfiguration.DESTINATION_LOCATION_ELEMENT
            ).ConnectValue(value => this.SpecificFolder = value);
            this.SettingsFactory = ComponentRegistry.Instance.GetComponent<BassEncoderSettingsFactory>();
            base.InitializeComponent(core);
        }

        public EncoderItem[] Create(IFileData[] fileDatas, string profile)
        {
            return fileDatas
                .OrderBy(fileData => fileData.FileName)
                .Select(fileData => Create(fileData, profile))
                .ToArray();
        }

        public EncoderItem Create(IFileData fileData, string profile)
        {
            var outputFileName = this.GetOutputFileName(fileData, profile);
            return EncoderItem.Create(fileData.FileName, outputFileName, fileData.MetaDatas, profile);
        }

        protected virtual string GetOutputFileName(IFileData fileData, string profile)
        {
            var settings = default(IBassEncoderSettings);
            if (!this.Settings.TryGetValue(profile, out settings))
            {
                settings = this.SettingsFactory.CreateSettings(profile);
                this.Settings[profile] = settings;
            }
            var extension = settings.Extension;
            var name = Path.GetFileNameWithoutExtension(fileData.FileName);
            var directoryName = default(string);
            switch (this.Destination)
            {
                default:
                case BassEncoderOutputDestination.Browse:
                    if (!this.GetBrowseFolder(fileData.FileName, out directoryName))
                    {
                        throw new OperationCanceledException();
                    }
                    break;
                case BassEncoderOutputDestination.Source:
                    directoryName = Path.GetDirectoryName(fileData.FileName);
                    break;
                case BassEncoderOutputDestination.Specific:
                    directoryName = this.SpecificFolder;
                    break;
            }
            if (!this.CanWrite(directoryName))
            {
                throw new InvalidOperationException(string.Format("Cannot output to path \"{0}\" please check encoder settings.", directoryName));
            }
            return Path.Combine(directoryName, string.Format("{0}.{1}", name, extension));
        }

        protected virtual bool CanWrite(string directoryName)
        {
            var uri = default(Uri);
            if (!Uri.TryCreate(directoryName, UriKind.Absolute, out uri))
            {
                return false;
            }
            return string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase);
        }
    }
}
