﻿using System;
using System.Collections.ObjectModel;

namespace FoxTunes
{
    [Serializable]
    public abstract class ConfigurationElement : BaseComponent
    {
        private ConfigurationElement()
        {
            this.IsHidden = false;
        }

        protected ConfigurationElement(string id, string name = null, string description = null) : this()
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        private bool _IsHidden { get; set; }

        public bool IsHidden
        {
            get
            {
                return this._IsHidden;
            }
            set
            {
                this._IsHidden = value;
                this.OnIsHiddenChanged();
            }
        }

        protected virtual void OnIsHiddenChanged()
        {
            if (this.IsHiddenChanged != null)
            {
                this.IsHiddenChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsHidden");
        }

        [field: NonSerialized]
        public event EventHandler IsHiddenChanged = delegate { };

        [field: NonSerialized]
        private ObservableCollection<ValidationRule> _ValidationRules;

        public ObservableCollection<ValidationRule> ValidationRules
        {
            get
            {
                return this._ValidationRules;
            }
            set
            {
                this._ValidationRules = value;
                this.OnValidationRulesChanged();
            }
        }

        protected virtual void OnValidationRulesChanged()
        {
            if (this.ValidationRulesChanged != null)
            {
                this.ValidationRulesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ValidationRules");
        }

        [field: NonSerialized]
        public event EventHandler ValidationRulesChanged = delegate { };

        public ConfigurationElement WithValidationRule(ValidationRule validationRule)
        {
            if (this.ValidationRules == null)
            {
                this.ValidationRules = new ObservableCollection<ValidationRule>();
            }
            this.ValidationRules.Add(validationRule);
            return this;
        }

        public abstract ConfigurationElement ConnectValue<T>(Action<T> action);

        public void Update(ConfigurationElement element)
        {
            this.Name = element.Name;
            this.Description = element.Description;
            this.IsHidden = false;
            this.ValidationRules = element.ValidationRules;
            this.OnUpdate(element);
        }

        protected virtual void OnUpdate(ConfigurationElement element)
        {
            //Nothing to do.
        }

        public ConfigurationElement Hide()
        {
            this.IsHidden = true;
            return this;
        }

        public ConfigurationElement Show()
        {
            this.IsHidden = false;
            return this;
        }
    }
}
