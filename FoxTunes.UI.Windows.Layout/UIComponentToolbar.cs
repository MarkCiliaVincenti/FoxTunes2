﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    [UIComponent("79EB216A-2BB0-40D9-8CF3-64645BCA50A2", role: UIComponentRole.System)]
    public class UIComponentToolbar : UIComponentPanel
    {
        public const string Alignment = "Alignment";

        public UIComponentToolbar()
        {
            this.Grid = new Grid();
            this.Content = this.Grid;
            this.ContextMenu = new Menu()
            {
                Components = new ObservableCollection<IInvocableComponent>(new[] { this }),
                Source = this,
                ExplicitOrdering = true
            };
        }

        public Grid Grid { get; private set; }

        public IEnumerable<KeyValuePair<UIComponent, UIComponentToolbarAttribute>> GetComponents()
        {
            var components = new Dictionary<UIComponent, UIComponentToolbarAttribute>();
            foreach (var component in LayoutManager.Instance.Components)
            {
                var attribute = default(UIComponentToolbarAttribute);
                if (component.Type.HasCustomAttribute<UIComponentToolbarAttribute>(out attribute))
                {
                    components.Add(component, attribute);
                }
            }
            return components.OrderBy(pair => pair.Value.Sequence);
        }

        public IEnumerable<KeyValuePair<UIComponent, UIComponentToolbarAttribute>> GetDefaultComponents()
        {
            return this.GetComponents().Where(pair => pair.Value.Default);
        }

        public bool HasComponent(UIComponent component)
        {
            var configuration = default(UIComponentConfiguration);
            return this.HasComponent(component, out configuration);
        }

        public bool HasComponent(UIComponent component, out UIComponentConfiguration configuration)
        {
            configuration = this.Configuration.Children.FirstOrDefault(child => string.Equals(child.Component.Id, component.Id, StringComparison.OrdinalIgnoreCase));
            return configuration != null;
        }

        protected override void OnConfigurationChanged()
        {
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateChildren()
        {
            this.Grid.Children.Clear(UIDisposerFlags.Default);
            this.Grid.ColumnDefinitions.Clear();
            if (this.Configuration.Children.Count == 0)
            {
                foreach (var component in this.GetDefaultComponents())
                {
                    this.AddChild(component.Key, component.Value);
                }
            }
            foreach (var pair in this.GetComponents())
            {
                var configuration = default(UIComponentConfiguration);
                if (this.HasComponent(pair.Key, out configuration))
                {
                    switch (pair.Value.Alignment)
                    {
                        case UIComponentToolbarAlignment.Left:
                            this.AddLeft(new[] { configuration });
                            break;
                        case UIComponentToolbarAlignment.Stretch:
                            this.AddStretch(new[] { configuration });
                            break;
                        case UIComponentToolbarAlignment.Right:
                            this.AddRight(new[] { configuration });
                            break;
                    }
                }
            }
        }

        protected virtual void AddChild(UIComponent component, UIComponentToolbarAttribute attribute)
        {
            var child = new UIComponentConfiguration(component);
            child.MetaData.AddOrUpdate(Alignment, Enum.GetName(typeof(UIComponentToolbarAlignment), attribute.Alignment));
            this.Configuration.Children.Add(child);
        }

        protected virtual void RemoveChild(UIComponent component)
        {
            for (var a = this.Configuration.Children.Count - 1; a >= 0; a--)
            {
                if (string.Equals(this.Configuration.Children[a].Component.Id, component.Id, StringComparison.OrdinalIgnoreCase))
                {
                    this.Configuration.Children.RemoveAt(a);
                }
            }
        }

        protected virtual void AddLeft(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Left, new GridLength(0, GridUnitType.Auto));
        }

        protected virtual void AddStretch(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Stretch, new GridLength(1, GridUnitType.Star));
        }

        protected virtual void AddRight(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Right, new GridLength(0, GridUnitType.Auto));
        }

        protected virtual void AddContainer(IEnumerable<UIComponentConfiguration> components, HorizontalAlignment alignment, GridLength width)
        {
            if (!components.Any())
            {
                //Create an empty column so other things align correctly.
                this.Grid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = width
                });
                return;
            }
            foreach (var component in components)
            {
                this.AddContainer(alignment, width, component);
            }
        }

        protected virtual void AddContainer(HorizontalAlignment alignment, GridLength width, UIComponentConfiguration component)
        {
            var margin = default(Thickness);
            if (this.Grid.ColumnDefinitions.Count > 0)
            {
                margin = new Thickness(2, 0, 0, 0);
            }
            this.Grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = width
            });
            var container = new UIComponentContainer()
            {
                Configuration = component,
                Margin = margin,
                HorizontalAlignment = alignment,
            };
            //TODO: Don't like anonymous event handlers, they can't be unsubscribed.
            container.ConfigurationChanged += (sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            };
            Grid.SetColumn(container, this.Grid.ColumnDefinitions.Count - 1);
            this.Grid.Children.Add(container);
        }

        protected virtual void UpdateComponent(UIComponentConfiguration originalComponent, UIComponentConfiguration newComponent)
        {
            for (var a = 0; a < this.Configuration.Children.Count; a++)
            {
                if (!object.ReferenceEquals(this.Configuration.Children[a], originalComponent))
                {
                    continue;
                }
                this.Configuration.Children[a] = newComponent;
                this.UpdateChildren();
                return;
            }
            //TODO: Component was not found.
            throw new NotImplementedException();
        }

        protected virtual void OnComponentChanged(object sender, EventArgs e)
        {
            var container = sender as UIComponentContainer;
            if (container == null)
            {
                return;
            }
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_GLOBAL;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                foreach (var component in this.GetComponents())
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_GLOBAL,
                        component.Key.Id,
                        component.Key.Name,
                        attributes: this.HasComponent(component.Key) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            return Windows.Invoke(() =>
            {
                var pair = this.GetComponents().FirstOrDefault(_pair => string.Equals(_pair.Key.Id, component.Id, StringComparison.OrdinalIgnoreCase));
                if (this.HasComponent(pair.Key))
                {
                    this.RemoveChild(pair.Key);
                }
                else
                {
                    this.AddChild(pair.Key, pair.Value);
                }
                this.UpdateChildren();
            });
        }
    }
}
