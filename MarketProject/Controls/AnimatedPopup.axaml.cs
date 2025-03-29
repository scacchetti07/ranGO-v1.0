using System;
using System.Timers;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace MarketProject.Controls;

// Imported by Milksove Project
public partial class AnimatedPopup : UserControl
{
    public static readonly StyledProperty<PlacementMode> PlacementModeProperty =
        AvaloniaProperty.Register<AnimatedPopup, PlacementMode>(nameof(PlacementMode), PlacementMode.Absolute);

    public static readonly StyledProperty<Control?> PlacementTargetProperty =
        AvaloniaProperty.Register<AnimatedPopup, Control?>(nameof(PlacementTarget));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AnimatedPopup, bool>(nameof(IsOpen));

    public static readonly StyledProperty<Control?> ChildProperty =
        AvaloniaProperty.Register<AnimatedPopup, Control?>(nameof(Child));

    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<AnimatedPopup, TimeSpan>(nameof(AnimationDuration), TimeSpan.FromMilliseconds(200));

    public static readonly StyledProperty<TimeSpan?> CloseAfterProperty =
        AvaloniaProperty.Register<AnimatedPopup, TimeSpan?>(nameof(CloseAfter));

    public PlacementMode PlacementMode
    {
        get => GetValue(PlacementModeProperty);
        set => SetValue(PlacementModeProperty, value);
    }

    public Control? PlacementTarget
    {
        get => GetValue(PlacementTargetProperty);
        set => SetValue(PlacementTargetProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public Panel? ChildContainer => Child.Parent as Panel;

    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public TimeSpan? CloseAfter
    {
        get => GetValue(CloseAfterProperty);
        set => SetValue(CloseAfterProperty, value);
    }

    private Point? _currentPointerPosition;

    private DispatcherTimer? _closeAfterTimer;

    public AnimatedPopup()
    {
        InitializeComponent();
        UpdateAnimationDuration();
        PlacementModeProperty.Changed.AddClassHandler<AnimatedPopup>((x, _) => x.UpdatePopup());
        PlacementTargetProperty.Changed.AddClassHandler<AnimatedPopup>((x, _) => x.UpdatePopup());
        ChildProperty.Changed.AddClassHandler<AnimatedPopup>((x, _) => x.UpdatePopup());
        IsOpenProperty.Changed.AddClassHandler<AnimatedPopup>(
            (x, e) =>
            {
                bool isOpen = (bool)e.NewValue;
                if (isOpen)
                    x.Open();
                else
                    x.Hide();
            }
        );
        AnimationDurationProperty.Changed.AddClassHandler<AnimatedPopup>((x, _) => x.UpdateAnimationDuration());
    }

    private void Open()
    {
        if (ChildContainer is null)
            return;

        ChildContainer.IsHitTestVisible = true;

        if (PlacementMode == PlacementMode.Pointer)
        {
            if (_currentPointerPosition is null)
                return;
            Canvas.SetLeft(ChildContainer, _currentPointerPosition.Value.X);
            Canvas.SetTop(ChildContainer, _currentPointerPosition.Value.Y);
        }

        ChildContainer.Opacity = 1;
        
        if (CloseAfter is null) return;
        
        _closeAfterTimer?.Stop();
        _closeAfterTimer = new DispatcherTimer { Interval = CloseAfter.Value };
        _closeAfterTimer.Tick += (_, _) =>
        {
            IsOpen = false;
            ChildContainer.IsHitTestVisible = false;
            _closeAfterTimer.Stop();
        };
        _closeAfterTimer.Start();
    }

    private void Hide()
    {
        if (ChildContainer is null)
            return;

        ChildContainer.Opacity = 0;
    }

    private void PointerMoved(object? sender, PointerEventArgs args)
    {
        if (PlacementMode != PlacementMode.Pointer || Content is not Control control)
            return;
        _currentPointerPosition = args.GetPosition(control);
    }

    private void UpdatePopup()
    {
        switch (PlacementMode)
        {
            case PlacementMode.Pointer:
                UpdatePopupPointer();
                return;
            case PlacementMode.Center:
                UpdatePopupCenter();
                return;
            case PlacementMode.Absolute:
                UpdatePopupAbsolute();
                return;
            default:
                UpdatePopupDefault();
                return;
        }
    }

    private void UpdatePopupPointer()
    {
        if (Child is null)
            return;
        Canvas canvas = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
        };
        Window parentWindow = this.FindLogicalAncestorOfType<Window>();
        parentWindow.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);
        parentWindow.AddHandler(InputElement.PointerMovedEvent, PointerMoved, RoutingStrategies.Tunnel);
        canvas.Background = Brushes.Transparent;
        canvas.IsHitTestVisible = false;

        Panel childContainer = CreateChildContainer(Child);

        canvas.Children.Add(childContainer);
        Content = canvas;
    }

    private void UpdatePopupCenter()
    {
        if (PlacementTarget is null || Child is null)
            return;
        Panel panel = new();
        panel.Children.Add(PlacementTarget);

        Panel childContainer = CreateChildContainer(Child);

        panel.Children.Add(childContainer);
        PlacementTarget.ZIndex = 0;
        childContainer.ZIndex = 100;
        Content = panel;
    }

    private void UpdatePopupAbsolute()
    {
        if (Child is null)
            return;

        Panel childContainer = CreateChildContainer(Child);

        Content = childContainer;
    }

    private void UpdatePopupDefault()
    {
        if (PlacementTarget is null || Child is null)
            return;
        Orientation orientation;
        if (PlacementMode == PlacementMode.Left || PlacementMode == PlacementMode.Right)
            orientation = Orientation.Horizontal;
        else
            orientation = Orientation.Vertical;
        StackPanel stackPanel = new() { Orientation = orientation };

        stackPanel.Children.Add(PlacementTarget);

        Panel childContainer = CreateChildContainer(Child);

        if (PlacementMode == PlacementMode.Left || PlacementMode == PlacementMode.Top)
            stackPanel.Children.Insert(0, childContainer);
        else
            stackPanel.Children.Add(childContainer);
        Content = stackPanel;
    }

    private Panel CreateChildContainer(Control child)
    {
        if (child.Parent is Panel panel)
            panel.Children.Remove(child);
        Panel childContainer = new() { Opacity = IsOpen ? 1 : 0, IsHitTestVisible = IsOpen };
        childContainer.Classes.Add("childContainer");
        AnimatedPopup popup = this;
        DispatcherTimer timer = null;
        OpacityProperty.Changed.AddClassHandler<Panel>(
            (x, e) =>
            {
                if (x == childContainer)
                {
                    timer?.Stop();
                    timer = new DispatcherTimer { Interval = popup.AnimationDuration };
                    timer.Tick += (_, _) =>
                    {
                        Dispatcher.UIThread.Invoke(AnimationEnded);
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        );

        childContainer.Children.Add(child);
        return childContainer;
    }

    private void AnimationEnded()
    {
        if (PlacementTarget is null || ChildContainer is null)
            return;
        if (!IsOpen)
            ChildContainer.IsHitTestVisible = false;
    }


    private void UpdateAnimationDuration()
    {
        Styles.Add(
            new Style()
            {
                Selector = Selectors.OfType<Panel>(null).Class("childContainer"),
                Setters =
                {
                    new Setter()
                    {
                        Property = TransitionsProperty,
                        Value = new Transitions()
                        {
                            new DoubleTransition() { Property = OpacityProperty, Duration = AnimationDuration }
                        }
                    }
                }
            }
        );
    }
}

public enum PlacementMode
{
    Top,
    Bottom,
    Left,
    Right,
    Center,
    Pointer,
    Absolute
}