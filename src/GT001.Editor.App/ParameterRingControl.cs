using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace GT001.Editor.App;

public sealed class ParameterRingControl : Grid
{
    private const double RingSize = 66;
    private const double RingStroke = 6;

    private readonly Microsoft.UI.Xaml.Shapes.Path _valueArc;
    private readonly Microsoft.UI.Xaml.Shapes.Ellipse _zeroDot;
    private readonly TextBlock _valueText;
    private readonly TextBlock _labelText;
    private readonly int _resetValue;
    private readonly Func<int, string> _formatValue;
    private readonly bool _isCentered;
    private Point _dragStartPoint;
    private int _dragStartValue;
    private bool _isDragging;
    private bool _changedDuringPointerGesture;

    public ParameterRingControl(
        string label,
        int minimum,
        int maximum,
        int value,
        int resetValue,
        Func<int, string>? formatValue = null,
        bool isCentered = false)
    {
        Minimum = minimum;
        Maximum = maximum;
        Value = CoerceValue(value);
        _resetValue = CoerceValue(resetValue);
        _formatValue = formatValue ?? (rawValue => rawValue.ToString());
        _isCentered = isCentered;

        Width = 92;
        MinHeight = 100;
        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        Padding = new Thickness(4, 2, 4, 0);
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(RingSize) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var ringGrid = new Grid
        {
            Width = RingSize,
            Height = RingSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0))
        };

        ringGrid.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 63, 88)),
            StrokeThickness = RingStroke
        });

        _valueArc = new Microsoft.UI.Xaml.Shapes.Path
        {
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 182, 77)),
            StrokeThickness = RingStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        ringGrid.Children.Add(_valueArc);

        _zeroDot = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = RingStroke + 1,
            Height = RingStroke + 1,
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 182, 77)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = isCentered ? Visibility.Visible : Visibility.Collapsed
        };
        ringGrid.Children.Add(_zeroDot);

        _valueText = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        ringGrid.Children.Add(_valueText);

        Children.Add(ringGrid);

        _labelText = new TextBlock
        {
            Text = label,
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 5, 0, 0)
        };
        Grid.SetRow(_labelText, 1);
        Children.Add(_labelText);

        PointerPressed += ParameterRingControl_PointerPressed;
        PointerMoved += ParameterRingControl_PointerMoved;
        PointerReleased += ParameterRingControl_PointerReleased;
        PointerCaptureLost += ParameterRingControl_PointerCaptureLost;
        DoubleTapped += ParameterRingControl_DoubleTapped;
        UpdateVisuals();
    }

    public event EventHandler<int>? ValueChangedByUser;
    public event EventHandler<int>? ValueCommitted;

    public int Minimum { get; }
    public int Maximum { get; }
    public int Value { get; private set; }

    public void SetValue(int value)
    {
        Value = CoerceValue(value);
        UpdateVisuals();
    }

    private void ParameterRingControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _changedDuringPointerGesture = false;
        _dragStartPoint = e.GetCurrentPoint(this).Position;
        _dragStartValue = Value;
        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void ParameterRingControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var point = e.GetCurrentPoint(this).Position;
        var verticalDelta = _dragStartPoint.Y - point.Y;
        var horizontalDelta = point.X - _dragStartPoint.X;
        var stepDelta = (int)Math.Round((verticalDelta + horizontalDelta * 0.35) / 4);
        SetUserValue(_dragStartValue + stepDelta, commit: false);
        e.Handled = true;
    }

    private void ParameterRingControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _changedDuringPointerGesture)
        {
            CommitValue();
        }

        _isDragging = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void ParameterRingControl_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _changedDuringPointerGesture)
        {
            CommitValue();
        }

        _isDragging = false;
        _changedDuringPointerGesture = false;
    }

    private void ParameterRingControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        SetUserValue(_resetValue, commit: true);
        e.Handled = true;
    }

    private void SetUserValue(int value, bool commit)
    {
        var coerced = CoerceValue(value);
        if (coerced == Value)
        {
            return;
        }

        Value = coerced;
        _changedDuringPointerGesture |= _isDragging;
        UpdateVisuals();
        ValueChangedByUser?.Invoke(this, Value);

        if (commit)
        {
            CommitValue();
        }
    }

    private void CommitValue()
        => ValueCommitted?.Invoke(this, Value);

    private int CoerceValue(int value)
        => Math.Clamp(value, Minimum, Maximum);

    private void UpdateVisuals()
    {
        _valueText.Text = _formatValue(Value);

        var radius = (RingSize - RingStroke) / 2;
        var center = new Point(RingSize / 2, RingSize / 2);
        var startAngle = _isCentered ? 0 : 210;
        var sweepAngle = _isCentered ? GetCenteredSweepAngle() : Math.Max(0.01, GetRawProgress(Value) * 300);
        var endAngle = startAngle + sweepAngle;
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, endAngle);

        _valueArc.Data = new PathGeometry
        {
            Figures =
            {
                new PathFigure
                {
                    StartPoint = start,
                    Segments =
                    {
                        new ArcSegment
                        {
                            Point = end,
                            Size = new Size(radius, radius),
                            SweepDirection = sweepAngle >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                            IsLargeArc = Math.Abs(sweepAngle) > 180
                        }
                    }
                }
            }
        };

        var dotPoint = PointOnCircle(center, radius, 0);
        _zeroDot.Margin = new Thickness(
            dotPoint.X - (_zeroDot.Width / 2),
            dotPoint.Y - (_zeroDot.Height / 2),
            0,
            0);
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = (angleDegrees - 90) * Math.PI / 180;
        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    private double GetRawProgress(int value)
    {
        var range = Math.Max(1, Maximum - Minimum);
        return (value - Minimum) / (double)range;
    }

    private double GetCenteredSweepAngle()
    {
        var center = _resetValue;
        if (Value == center)
        {
            return 0.01;
        }

        var negativeRange = Math.Max(1, center - Minimum);
        var positiveRange = Math.Max(1, Maximum - center);
        return Value < center
            ? -150 * ((center - Value) / (double)negativeRange)
            : 150 * ((Value - center) / (double)positiveRange);
    }
}
