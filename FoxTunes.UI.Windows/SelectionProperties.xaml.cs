﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for SelectionProperties.xaml
    /// </summary>
    [UIComponent("1155473E-FA29-4D31-8A28-4E4F5582261A", role: UIComponentRole.Info)]
    public partial class SelectionProperties : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "B3DC576F-73EB-421F-B7DF-4EEB0CC2F5B8";

        public SelectionProperties()
        {
            this.InitializeComponent();
        }

        public BooleanConfigurationElement ShowTags { get; private set; }

        public BooleanConfigurationElement ShowProperties { get; private set; }

        public BooleanConfigurationElement ShowImages { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.ShowTags = this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_TAGS
                );
                this.ShowProperties = this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_PROPERTIES
                );
                this.ShowImages = this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_IMAGES
                );
            }
            base.OnConfigurationChanged();
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    CATEGORY,
                    this.ShowTags.Id,
                    this.ShowTags.Name,
                    attributes: this.ShowTags.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.ShowProperties.Id,
                    this.ShowProperties.Name,
                    attributes: this.ShowProperties.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.ShowImages.Id,
                    this.ShowImages.Name,
                    attributes: this.ShowImages.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(this.ShowTags.Name, component.Name))
            {
                this.ShowTags.Toggle();
            }
            else if (string.Equals(this.ShowProperties.Name, component.Name))
            {
                this.ShowProperties.Toggle();
            }
            else if (string.Equals(this.ShowImages.Name, component.Name))
            {
                this.ShowImages.Toggle();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task<bool> ShowSettings()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return SelectionPropertiesConfiguration.GetConfigurationSections();
        }
    }
}
