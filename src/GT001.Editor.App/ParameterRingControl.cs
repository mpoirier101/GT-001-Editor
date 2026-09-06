using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GT001.Editor.App;

public sealed class ParameterRingControl : Grid
{
    private const double RingSize = 66;
    private const double RingStroke = 6;
    private readonly Path _valueArc;
    private readonly Ellipse _zeroDot;
    private readonly TextBlock _valueText;
    private readonly int _resetValue;
    private readonly Func<int, string> _formatValue;
    private readonly bool _isCentered;
    private Point _dragStartPoint;
    private int _dragStartValue;
    private bool _isDragging;
    private bool _changedDuringMouseGesture;

    public ParameterRingControl(string label, int minimum, int maximum, int value, int resetValue, Func<int, string>? formatValue = null, bool isCentered = false)
    {
        Minimum = minimum;
        Maximum = maximum;
        Value = CoerceValue(value);
        _resetValue = CoerceValue(resetValue);
        _formatValue = formatValue ?? (rawValue => rawValue.ToString());
        _isCentered = isCentered;
        Width = 92;
        MinHeight = 100;
        Background = Brushes.Transparent;
        Margin = new Thickness(4, 2, 4, 0);
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(RingSize) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var ringGrid = new Grid { Width = RingSize, Height = RingSize, HorizontalAlignment = HorizontalAlignment.Center, Background = Brushes.Transparent };
        ringGrid.Children.Add(new Ellipse { Stroke = new SolidColorBrush(Color.FromRgb(45, 63, 88)), StrokeThickness = RingStroke });
        _valueArc = new Path { Stroke = new SolidColorBrush(Color.FromRgb(247, 182, 77)), StrokeThickness = RingStroke, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        ringGrid.Children.Add(_valueArc);
        _zeroDot = new Ellipse { Width = RingStroke + 1, Height = RingStroke + 1, Fill = new SolidColorBrush(Color.FromRgb(247, 182, 77)), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Visibility = isCentered ? Visibility.Visible : Visibility.Collapsed };
        ringGrid.Children.Add(_zeroDot);
        _valueText = new TextBlock { FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
        ringGrid.Children.Add(_valueText);
        Children.Add(ringGrid);
        var labelText = new TextBlock { Text = label, FontSize = 12, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 5, 0, 0) };
        Grid.SetRow(labelText, 1);
        Children.Add(labelText);
        MouseLeftButtonDown += ParameterRingControl_MouseLeftButtonDown;
        MouseMove += ParameterRingControl_MouseMove;
        MouseLeftButtonUp += ParameterRingControl_MouseLeftButtonUp;
        LostMouseCapture += ParameterRingControl_LostMouseCapture;
        UpdateVisuals();
    }

    public event EventHandler<int>? ValueChangedByUser;
    public event EventHandler<int>? ValueCommitted;
    public int Minimum { get; }
    public int Maximum { get; }
    public int Value { get; private set; }
    public void SetValue(int value) { Value = CoerceValue(value); UpdateVisuals(); }

    private void ParameterRingControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) { SetUserValue(_resetValue, true); e.Handled = true; return; } _isDragging = true; _changedDuringMouseGesture = false; _dragStartPoint = e.GetPosition(this); _dragStartValue = Value; CaptureMouse(); e.Handled = true; }
    private void ParameterRingControl_MouseMove(object sender, MouseEventArgs e) { if (!_isDragging) return; var point = e.GetPosition(this); var stepDelta = (int)Math.Round((_dragStartPoint.Y - point.Y + (point.X - _dragStartPoint.X) * 0.35) / 4); SetUserValue(_dragStartValue + stepDelta, false); e.Handled = true; }
    private void ParameterRingControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (_isDragging && _changedDuringMouseGesture) CommitValue(); _isDragging = false; ReleaseMouseCapture(); e.Handled = true; }
    private void ParameterRingControl_LostMouseCapture(object sender, MouseEventArgs e) { if (_isDragging && _changedDuringMouseGesture) CommitValue(); _isDragging = false; _changedDuringMouseGesture = false; }
    private void SetUserValue(int value, bool commit) { var coerced = CoerceValue(value); if (coerced == Value) return; Value = coerced; _changedDuringMouseGesture |= _isDragging; UpdateVisuals(); ValueChangedByUser?.Invoke(this, Value); if (commit) CommitValue(); }
    private void CommitValue() => ValueCommitted?.Invoke(this, Value);
    private int CoerceValue(int value) => Math.Clamp(value, Minimum, Maximum);
    private void UpdateVisuals()
    {
        _valueText.Text = _formatValue(Value);
        var radius = (RingSize - RingStroke) / 2;
        var center = new Point(RingSize / 2, RingSize / 2);
        var startAngle = _isCentered ? 0 : 210;
        var sweepAngle = _isCentered ? GetCenteredSweepAngle() : Math.Max(0.01, GetRawProgress(Value) * 300);
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweepAngle);
        _valueArc.Data = new PathGeometry { Figures = { new PathFigure { StartPoint = start, Segments = { new ArcSegment { Point = end, Size = new Size(radius, radius), SweepDirection = sweepAngle >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, IsLargeArc = Math.Abs(sweepAngle) > 180 } } } } };
        var dotPoint = PointOnCircle(center, radius, 0);
        _zeroDot.Margin = new Thickness(dotPoint.X - _zeroDot.Width / 2, dotPoint.Y - _zeroDot.Height / 2, 0, 0);
    }
    private static Point PointOnCircle(Point center, double radius, double angleDegrees) { var radians = (angleDegrees - 90) * Math.PI / 180; return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians)); }
    private double GetRawProgress(int value) => (value - Minimum) / (double)Math.Max(1, Maximum - Minimum);
    private double GetCenteredSweepAngle() { if (Value == _resetValue) return 0.01; return Value < _resetValue ? -150 * ((_resetValue - Value) / (double)Math.Max(1, _resetValue - Minimum)) : 150 * ((Value - _resetValue) / (double)Math.Max(1, Maximum - _resetValue)); }
}
