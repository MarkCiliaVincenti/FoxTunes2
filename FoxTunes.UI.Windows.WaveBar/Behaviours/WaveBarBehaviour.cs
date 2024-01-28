﻿using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class WaveBarBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public const string CATEGORY = "0E698392-FF2C-415A-BB6E-754604DFAB57";

        public ThemeLoader ThemeLoader { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public BooleanConfigurationElement Rms { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            this.Configuration = core.Components.Configuration;
            this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                WaveBarBehaviourConfiguration.SECTION,
                WaveBarBehaviourConfiguration.MODE_ELEMENT
            );
            this.Rms = this.Configuration.GetElement<BooleanConfigurationElement>(
                WaveBarBehaviourConfiguration.SECTION,
                WaveBarBehaviourConfiguration.RMS_ELEMENT
            );
            this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                WaveBarBehaviourConfiguration.SECTION,
                WaveBarBehaviourConfiguration.COLOR_PALETTE_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                foreach (var option in this.Mode.Options)
                {
                    yield return new InvocationComponent(
                        CATEGORY,
                        option.Id,
                        option.Name,
                        path: this.Mode.Name,
                        attributes: this.Mode.Value == option ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE);
                }
                if (!this.Rms.Value)
                {
                    foreach (var component in this.ThemeLoader.SelectColorPalette(CATEGORY, this.ColorPalette))
                    {
                        yield return component;
                    }
                }
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Rms.Id,
                    this.Rms.Name,
                    attributes: this.Rms.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE);
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(this.Mode.Name, component.Path))
            {
                this.Mode.Value = this.Mode.Options.FirstOrDefault(option => string.Equals(option.Id, component.Id));
            }
            else if (string.Equals(this.Rms.Name, component.Name))
            {
                this.Rms.Toggle();
            }
            else if (this.ThemeLoader.SelectColorPalette(this.ColorPalette, component))
            {
                //Nothing to do.
            }
            this.Configuration.Save();
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WaveBarBehaviourConfiguration.GetConfigurationSections();
        }
    }
}