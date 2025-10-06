using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData.Binding;
using Foobar.ViewModels;
using Point = Foobar.Models.Point;

namespace Foobar.Views;

public class DrawingCanvas : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<DrawingCanvas, IBrush?>(nameof(Background));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Avalonia.Point Translation { get; set; } = new(300, 300);

    public static readonly StyledProperty<ReadOnlyObservableCollection<Point>> PointsProperty =
        AvaloniaProperty.Register<DrawingCanvas, ReadOnlyObservableCollection<Point>>(nameof(Points));
    public ReadOnlyObservableCollection<Point> Points
    {
        private get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public enum ELineStyle
    {
        PolyLine,
        CubicBezier,
    }

    public static readonly StyledProperty<ELineStyle> LineStyleProperty =
        AvaloniaProperty.Register<DrawingCanvas, ELineStyle>(nameof(LineStyle));
    public ELineStyle LineStyle
    {
        get => GetValue(LineStyleProperty);
        set => SetValue(LineStyleProperty, value);
    }

    bool _isPressed = false;
    Avalonia.Point _prevPos;
    Avalonia.Point _oldTranslation;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Console.WriteLine("Pressed");
        _isPressed = true;
        _oldTranslation = Translation;
        _prevPos = e.GetPosition(this);
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Console.WriteLine("Released");
        _isPressed = false;
        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isPressed)
        {
            var currPos = e.GetPosition(this);
            Translation = _oldTranslation - _prevPos + currPos;
            // Console.WriteLine($"{Translation.X}, {Translation.Y}");
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    public DrawingCanvas()
    {
        AffectsRender<DrawingCanvas>(
            BackgroundProperty,
             LineStyleProperty,
                PointsProperty
        );
    }

    /// <summary>
    /// Найти точки, которые влияют на отрисовку кривой:
    /// точки которые попадают на экран по оси X
    /// плюс 1 с каждой стороны
    /// </summary>
    /// <returns></returns>
    (int x0, int x1) FindNeedToDraw()
    {
        var left = -Translation.X;
        var right = -Translation.X + Bounds.Right;
        int x0 = 0;
        for (int i0 = 0; i0 < Points.Count; i0++)
        {
            if (Points[i0].X >= left)
            {
                x0 = i0 - 1;
                break;
            }
        }
        if (x0 < 0)
        {
            x0 = 0;
        }
        int x1 = Points.Count - 1;
        for (int i1 = x0; i1 < Points.Count; i1++)
        {
            if (Points[i1].X >= right)
            {
                x1 = i1;
                break;
            }
        }

        return (x0, x1);
    }

    public override void Render(DrawingContext context)
    {
        if (Background != null)
        {
            context.FillRectangle(Background, Bounds);
        }

        var m = new Matrix(1, 0, 0, -1, Translation.X, Translation.Y);
        context.PushTransform(m);
        var b = FindNeedToDraw();

        if (b.x0 < b.x1)
        {
            var br = new SolidColorBrush(Color.FromRgb(150, 0, 255));
            var pen = new Pen(br, 10, lineCap: PenLineCap.Round);

            switch (LineStyle)
            {
                case ELineStyle.PolyLine:
                    context.DrawGeometry(
                        br, pen,
                        new PolylineGeometry(Points.Select(x => x.AsAvalonian()), false)
                    );
                    break;
                case ELineStyle.CubicBezier:
                    var pathFigures = new PathFigures();
                    var pathFigure = new PathFigure()
                    {
                        StartPoint = Points[b.x0].AsAvalonian(),
                        IsFilled = false,
                        IsClosed = false
                    };
                    var segments = new PathSegments();
                    pathFigure.Segments = segments;
                    pathFigures.Add(pathFigure);

                    for (int i = b.x0; i < b.x1; i++)
                    {
                        var hx = (Points[i + 1].X - Points[i].X) / 2.0;
                        var hy = (Points[i + 1].Y - Points[i].Y) / 2.0;
                        segments.Add(new BezierSegment
                        {
                            Point1 = Points[i].AsAvalonian() + new Avalonia.Point(hx, hy / 2),
                            Point2 = Points[i + 1].AsAvalonian() - new Avalonia.Point(hx, hy / 2),
                            Point3 = Points[i + 1].AsAvalonian()
                        });
                    }
                    context.DrawGeometry(
                        br, pen,
                        new PathGeometry { Figures = pathFigures }
                    );
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        base.Render(context);
        Console.Write(".");
    }
}