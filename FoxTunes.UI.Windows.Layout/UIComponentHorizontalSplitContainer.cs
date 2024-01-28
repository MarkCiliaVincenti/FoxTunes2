﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for UIComponentHorizontalSplitContainer.xaml
    /// </summary>
    [UIComponent("A6820FDA-E415-40C6-AEFB-A73B6FBE4C93", UIComponentSlots.NONE, "Horizontal Split", role: UIComponentRole.Hidden)]
    public partial class UIComponentHorizontalSplitContainer : UIComponentPanel
    {
        const string FREEZE_TOP = "AAAA";

        const string FREEZE_BOTTOM = "BBBB";

        const string COLLAPSE_TOP = "CCCC";

        const string COLLAPSE_BOTTOM = "DDDD";

        const string DirectionTop = "Top";

        const string DirectionBottom = "Bottom";

        public static readonly DependencyProperty TopComponentProperty = DependencyProperty.Register(
            "TopComponent",
            typeof(UIComponentConfiguration),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnTopComponentChanged))
        );

        public static UIComponentConfiguration GetTopComponent(UIComponentHorizontalSplitContainer source)
        {
            return (UIComponentConfiguration)source.GetValue(TopComponentProperty);
        }

        public static void SetTopComponent(UIComponentHorizontalSplitContainer source, UIComponentConfiguration value)
        {
            source.SetValue(TopComponentProperty, value);
        }

        public static void OnTopComponentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnTopComponentChanged();
        }

        public static readonly DependencyProperty BottomComponentProperty = DependencyProperty.Register(
            "BottomComponent",
            typeof(UIComponentConfiguration),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnBottomComponentChanged))
        );

        public static UIComponentConfiguration GetBottomComponent(UIComponentHorizontalSplitContainer source)
        {
            return (UIComponentConfiguration)source.GetValue(BottomComponentProperty);
        }

        public static void SetBottomComponent(UIComponentHorizontalSplitContainer source, UIComponentConfiguration value)
        {
            source.SetValue(BottomComponentProperty, value);
        }

        public static void OnBottomComponentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnBottomComponentChanged();
        }

        public static readonly DependencyProperty TopEnabledProperty = DependencyProperty.Register(
            "TopEnabled",
            typeof(bool),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnTopEnabledChanged))
        );

        public static bool GetTopEnabled(UIComponentHorizontalSplitContainer source)
        {
            return (bool)source.GetValue(TopEnabledProperty);
        }

        public static void SetTopEnabled(UIComponentHorizontalSplitContainer source, bool value)
        {
            source.SetValue(TopEnabledProperty, value);
        }

        public static void OnTopEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnTopEnabledChanged();
        }

        public static readonly DependencyProperty BottomEnabledProperty = DependencyProperty.Register(
            "BottomEnabled",
            typeof(bool),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnBottomEnabledChanged))
        );

        public static bool GetBottomEnabled(UIComponentHorizontalSplitContainer source)
        {
            return (bool)source.GetValue(BottomEnabledProperty);
        }

        public static void SetBottomEnabled(UIComponentHorizontalSplitContainer source, bool value)
        {
            source.SetValue(BottomEnabledProperty, value);
        }

        public static void OnBottomEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnBottomEnabledChanged();
        }

        public static readonly DependencyProperty SplitterDistanceProperty = DependencyProperty.Register(
            "SplitterDistance",
            typeof(string),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnSplitterDistanceChanged))
        );

        public static string GetSplitterDistance(UIComponentHorizontalSplitContainer source)
        {
            return (string)source.GetValue(SplitterDistanceProperty);
        }

        public static void SetSplitterDistance(UIComponentHorizontalSplitContainer source, string value)
        {
            source.SetValue(SplitterDistanceProperty, value);
        }

        public static void OnSplitterDistanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnSplitterDistanceChanged();
        }

        public static readonly DependencyProperty SplitterDirectionProperty = DependencyProperty.Register(
            "SplitterDirection",
            typeof(string),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnSplitterDirectionChanged))
        );

        public static string GetSplitterDirection(UIComponentHorizontalSplitContainer source)
        {
            return (string)source.GetValue(SplitterDirectionProperty);
        }

        public static void SetSplitterDirection(UIComponentHorizontalSplitContainer source, string value)
        {
            source.SetValue(SplitterDirectionProperty, value);
        }

        public static void OnSplitterDirectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnSplitterDirectionChanged();
        }

        public static readonly DependencyProperty CollapseTopProperty = DependencyProperty.Register(
            "CollapseTop",
            typeof(bool),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnCollapseTopChanged))
        );

        public static bool GetCollapseTop(UIComponentHorizontalSplitContainer source)
        {
            return (bool)source.GetValue(CollapseTopProperty);
        }

        public static void SetCollapseTop(UIComponentHorizontalSplitContainer source, bool value)
        {
            source.SetValue(CollapseTopProperty, value);
        }

        public static void OnCollapseTopChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnCollapseTopChanged();
        }

        public static readonly DependencyProperty CollapseBottomProperty = DependencyProperty.Register(
            "CollapseBottom",
            typeof(bool),
            typeof(UIComponentHorizontalSplitContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnCollapseBottomChanged))
        );

        public static bool GetCollapseBottom(UIComponentHorizontalSplitContainer source)
        {
            return (bool)source.GetValue(CollapseBottomProperty);
        }

        public static void SetCollapseBottom(UIComponentHorizontalSplitContainer source, bool value)
        {
            source.SetValue(CollapseBottomProperty, value);
        }

        public static void OnCollapseBottomChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentHorizontalSplitContainer;
            if (container == null)
            {
                return;
            }
            container.OnCollapseBottomChanged();
        }

        public UIComponentHorizontalSplitContainer()
        {
            this.InitializeComponent();
        }

        public UIComponentContainer TopContainer { get; private set; }

        public UIComponentContainer BottomContainer { get; private set; }

        private void InitializeComponent()
        {
            this.TopContainer = new UIComponentContainer();
            this.BottomContainer = new UIComponentContainer();

            Grid.SetRow(this.TopContainer, 0);
            Grid.SetRow(this.BottomContainer, 2);

            this.TopContainer.SetBinding(
                UIComponentContainer.ComponentProperty,
                new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(this.TopComponent))
                }
            );
            this.BottomContainer.SetBinding(
                UIComponentContainer.ComponentProperty,
                new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(this.BottomComponent))
                }
            );

            this.SetBinding(
                UIComponentHorizontalSplitContainer.TopEnabledProperty,
                new Binding()
                {
                    Source = this.TopContainer,
                    Path = new PropertyPath("Content.IsComponentEnabled"),
                    FallbackValue = true
                }
            );
            this.SetBinding(
                UIComponentHorizontalSplitContainer.BottomEnabledProperty,
                new Binding()
                {
                    Source = this.BottomContainer,
                    Path = new PropertyPath("Content.IsComponentEnabled"),
                    FallbackValue = true
                }
            );
        }

        protected virtual void CreateLayout()
        {
            if (this.IsInDesignMode)
            {
                this.CreateSplitLayout();
                this.IsComponentEnabled = true;
                return;
            }
            var showTop = !this.CollapseTop || this.TopEnabled;
            var showBottom = !this.CollapseBottom || this.BottomEnabled;
            if (showTop && showBottom)
            {
                this.CreateSplitLayout();
                this.IsComponentEnabled = true;
            }
            else if (showTop)
            {
                this.CreateLayout(this.TopContainer);
                this.IsComponentEnabled = true;
            }
            else if (showBottom)
            {
                this.CreateLayout(this.BottomContainer);
                this.IsComponentEnabled = true;
            }
            else
            {
                this.Content = null;
                this.IsComponentEnabled = false;
            }
        }

        protected virtual void CreateSplitLayout()
        {
            if (this.Content is Grid)
            {
                //Nothing to do.
                return;
            }

            this.TopContainer.Disconnect();
            this.BottomContainer.Disconnect();

            var grid = new Grid();

            var topRow = new RowDefinition();
            var splitterRow = new RowDefinition();
            var bottomRow = new RowDefinition();

            var splitter = new GridSplitter();

            if (string.Equals(this.SplitterDirection, DirectionTop, StringComparison.OrdinalIgnoreCase))
            {
                topRow.SetBinding(
                    RowDefinition.HeightProperty,
                    new Binding()
                    {
                        Source = this,
                        Path = new PropertyPath(nameof(this.SplitterDistance)),
                        Converter = new global::FoxTunes.ViewModel.GridLengthConverter(),
                        Mode = BindingMode.TwoWay
                    }
                );
            }
            else
            {
                bottomRow.SetBinding(
                    RowDefinition.HeightProperty,
                    new Binding()
                    {
                        Source = this,
                        Path = new PropertyPath(nameof(this.SplitterDistance)),
                        Converter = new global::FoxTunes.ViewModel.GridLengthConverter(),
                        Mode = BindingMode.TwoWay
                    }
                );
            }

            splitterRow.Height = new GridLength(0, GridUnitType.Auto);

            Grid.SetRow(splitter, 1);
            splitter.Height = 4;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Center;

            grid.RowDefinitions.Add(topRow);
            grid.RowDefinitions.Add(splitterRow);
            grid.RowDefinitions.Add(bottomRow);

            grid.Children.Add(this.TopContainer);
            grid.Children.Add(splitter);
            grid.Children.Add(this.BottomContainer);

            this.Content = grid;
        }

        protected virtual void UpdateSplitterDirection()
        {
            if (this.Content is Grid grid)
            {
                var topRow = grid.RowDefinitions.FirstOrDefault();
                var bottomRow = grid.RowDefinitions.LastOrDefault();
                if (topRow == null || bottomRow == null || object.ReferenceEquals(topRow, bottomRow))
                {
                    return;
                }
                BindingOperations.ClearBinding(topRow, RowDefinition.HeightProperty);
                BindingOperations.ClearBinding(bottomRow, RowDefinition.HeightProperty);
                if (string.Equals(this.SplitterDirection, DirectionTop, StringComparison.OrdinalIgnoreCase))
                {
                    topRow.SetBinding(
                        RowDefinition.HeightProperty,
                        new Binding()
                        {
                            Source = this,
                            Path = new PropertyPath(nameof(this.SplitterDistance)),
                            Converter = new global::FoxTunes.ViewModel.GridLengthConverter(),
                            Mode = BindingMode.TwoWay
                        }
                    );
                    bottomRow.Height = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    topRow.Height = new GridLength(1, GridUnitType.Star);
                    bottomRow.SetBinding(
                       RowDefinition.HeightProperty,
                       new Binding()
                       {
                           Source = this,
                           Path = new PropertyPath(nameof(this.SplitterDistance)),
                           Converter = new global::FoxTunes.ViewModel.GridLengthConverter(),
                           Mode = BindingMode.TwoWay
                       }
                   );
                }
            }
        }

        protected virtual void CreateLayout(UIComponentContainer container)
        {
            if (object.ReferenceEquals(this.Content, container))
            {
                //Nothing to do.
                return;
            }
            container.Disconnect();
            this.Content = container;
        }


        protected override void OnIsInDesignModeChanged()
        {
            this.CreateLayout();
            base.OnIsInDesignModeChanged();
        }

        protected override void OnComponentChanged()
        {
            if (this.Component != null)
            {
                this.UpdateMetaData();
                this.UpdateChildren();
                this.CreateLayout();
            }
            base.OnComponentChanged();
        }

        protected virtual void UpdateChildren()
        {
            if (this.Component.Children != null && this.Component.Children.Count == 2)
            {
                this.TopComponent = this.Component.Children[0];
                this.BottomComponent = this.Component.Children[1];
            }
            else
            {
                this.Component.Children = new ObservableCollection<UIComponentConfiguration>()
                {
                    new UIComponentConfiguration(),
                    new UIComponentConfiguration()
                };
            }
        }

        protected virtual void UpdateMetaData()
        {
            var splitterDistance = default(string);
            var splitterDirection = default(string);
            var collapseTop = default(string);
            var collapseBottom = default(string);
            if (this.Component.TryGet(nameof(this.SplitterDistance), out splitterDistance))
            {
                this.SplitterDistance = splitterDistance;
            }
            else
            {
                this.SplitterDistance = "1*";
            }
            if (this.Component.TryGet(nameof(this.SplitterDirection), out splitterDirection))
            {
                this.SplitterDirection = splitterDirection;
            }
            else
            {
                this.SplitterDirection = DirectionBottom;
            }
            if (this.Component.TryGet(nameof(this.CollapseTop), out collapseTop))
            {
                this.CollapseTop = Convert.ToBoolean(collapseTop);
            }
            else
            {
                this.CollapseTop = false;
            }
            if (this.Component.TryGet(nameof(this.CollapseBottom), out collapseBottom))
            {
                this.CollapseBottom = Convert.ToBoolean(collapseBottom);
            }
            else
            {
                this.CollapseBottom = false;
            }
        }

        public UIComponentConfiguration TopComponent
        {
            get
            {
                return this.GetValue(TopComponentProperty) as UIComponentConfiguration;
            }
            set
            {
                this.SetValue(TopComponentProperty, value);
            }
        }

        protected virtual void OnTopComponentChanged()
        {
            if (this.Component != null && this.Component.Children.Count == 2 && this.TopComponent != null)
            {
                this.Component.Children[0] = this.TopComponent;
            }
            if (this.TopComponentChanged != null)
            {
                this.TopComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("TopComponent");
        }

        public event EventHandler TopComponentChanged;

        public UIComponentConfiguration BottomComponent
        {
            get
            {
                return this.GetValue(BottomComponentProperty) as UIComponentConfiguration;
            }
            set
            {
                this.SetValue(BottomComponentProperty, value);
            }
        }

        protected virtual void OnBottomComponentChanged()
        {
            if (this.Component != null && this.Component.Children.Count == 2 && this.BottomComponent != null)
            {
                this.Component.Children[1] = this.BottomComponent;
            }
            if (this.BottomComponentChanged != null)
            {
                this.BottomComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("BottomComponent");
        }

        public event EventHandler BottomComponentChanged;

        public bool TopEnabled
        {
            get
            {
                return (bool)this.GetValue(TopEnabledProperty);
            }
            set
            {
                this.SetValue(TopEnabledProperty, value);
            }
        }

        protected virtual void OnTopEnabledChanged()
        {
            this.CreateLayout();
            if (this.TopEnabledChanged != null)
            {
                this.TopEnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("TopEnabled");
        }

        public event EventHandler TopEnabledChanged;

        public bool BottomEnabled
        {
            get
            {
                return (bool)this.GetValue(BottomEnabledProperty);
            }
            set
            {
                this.SetValue(BottomEnabledProperty, value);
            }
        }

        protected virtual void OnBottomEnabledChanged()
        {
            this.CreateLayout();
            if (this.BottomEnabledChanged != null)
            {
                this.BottomEnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("BottomEnabled");
        }

        public event EventHandler BottomEnabledChanged;

        public string SplitterDistance
        {
            get
            {
                return (string)this.GetValue(SplitterDistanceProperty);
            }
            set
            {
                this.SetValue(SplitterDistanceProperty, value);
            }
        }

        protected virtual void OnSplitterDistanceChanged()
        {
            if (this.Component != null)
            {
                this.Component.AddOrUpdate(
                    nameof(this.SplitterDistance),
                    this.SplitterDistance
                );
            }
            if (this.SplitterDistanceChanged != null)
            {
                this.SplitterDistanceChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SplitterDistance");
        }

        public event EventHandler SplitterDistanceChanged;

        public string SplitterDirection
        {
            get
            {
                return (string)this.GetValue(SplitterDirectionProperty);
            }
            set
            {
                this.SetValue(SplitterDirectionProperty, value);
            }
        }

        protected virtual void OnSplitterDirectionChanged()
        {
            if (this.Component != null)
            {
                this.Component.AddOrUpdate(
                    nameof(this.SplitterDirection),
                    this.SplitterDirection
                );
            }
            this.UpdateSplitterDirection();
            if (this.SplitterDirectionChanged != null)
            {
                this.SplitterDirectionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SplitterDirection");
        }

        public event EventHandler SplitterDirectionChanged;


        public bool CollapseTop
        {
            get
            {
                return (bool)this.GetValue(CollapseTopProperty);
            }
            set
            {
                this.SetValue(CollapseTopProperty, value);
            }
        }

        protected virtual void OnCollapseTopChanged()
        {
            if (this.Component != null)
            {
                this.Component.AddOrUpdate(
                    nameof(this.CollapseTop),
                    Convert.ToString(this.CollapseTop)
                );
            }
            if (this.CollapseTopChanged != null)
            {
                this.CollapseTopChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CollapseTop");
        }

        public event EventHandler CollapseTopChanged;

        public bool CollapseBottom
        {
            get
            {
                return (bool)this.GetValue(CollapseBottomProperty);
            }
            set
            {
                this.SetValue(CollapseBottomProperty, value);
            }
        }

        protected virtual void OnCollapseBottomChanged()
        {
            if (this.Component != null)
            {
                this.Component.AddOrUpdate(
                    nameof(this.CollapseBottom),
                    Convert.ToString(this.CollapseBottom)
                );
            }
            if (this.CollapseBottomChanged != null)
            {
                this.CollapseBottomChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CollapseBottom");
        }

        public event EventHandler CollapseBottomChanged;

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    FREEZE_TOP,
                    "Freeze Top", attributes:
                    string.Equals(this.SplitterDirection, DirectionTop, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    FREEZE_BOTTOM,
                    "Freeze Bottom", attributes:
                    string.Equals(this.SplitterDirection, DirectionBottom, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    COLLAPSE_TOP,
                    "Collapsable Top", attributes:
                    (byte)((this.CollapseTop ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    COLLAPSE_BOTTOM,
                    "Collapsable Bottom", attributes:
                    this.CollapseBottom ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case FREEZE_TOP:
                    return Windows.Invoke(() => this.SplitterDirection = DirectionTop);
                case FREEZE_BOTTOM:
                    return Windows.Invoke(() => this.SplitterDirection = DirectionBottom);
                case COLLAPSE_TOP:
                    return Windows.Invoke(() => this.CollapseTop = !this.CollapseTop);
                case COLLAPSE_BOTTOM:
                    return Windows.Invoke(() => this.CollapseBottom = !this.CollapseBottom);
            }
            return base.InvokeAsync(component);
        }
    }
}
