﻿using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class MiniPlayer : ViewModelBase
    {
        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement _Enabled { get; private set; }

        public BooleanConfigurationElement Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                this.OnEnabledChanged();
            }
        }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged = delegate { };

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = this.Core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                MiniPlayerBehaviourConfiguration.SECTION,
                MiniPlayerBehaviourConfiguration.ENABLED_ELEMENT
            );
            base.InitializeComponent(core);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MiniPlayer();
        }
    }
}