using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Foobar.Models;
using ReactiveUI;
using Point = Foobar.Models.Point;

namespace Foobar.Views;

using Tables = ObservableCollection<TableModel>;

public class DrawingCanvas : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<DrawingCanvas, IBrush?>(nameof(Background));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<Tables> TablesProperty =
        AvaloniaProperty.Register<DrawingCanvas, Tables>(nameof(Tables));
    public Tables Tables
    {
        private get => GetValue(TablesProperty);
        set {
            SubscribeTo(value);
            value.CollectionChanged += (_, args) => SubscribeTo(args.NewItems);
            SetValue(TablesProperty, value);
        }
    }

    void SubscribeTo(IList tables)
    {
        foreach (TableModel table in tables)
        {
            table.WhenAnyValue(x => x.Color)
                .Subscribe(_ => InvalidateVisual());
        }
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
    Avalonia.Point _curPosition;
    Matrix _translationMatrix;

    Avalonia.Point _translation;
    public Avalonia.Point Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            _translationMatrix = Matrix.CreateTranslation(_translation);
            InvalidateVisual();
        }
    }

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
        _curPosition = e.GetPosition(this);
        // InvalidateVisual();
        if (_isPressed)
        {
            Translation = _oldTranslation - (_prevPos - _curPosition) / ScaleFactor;
        }
        base.OnPointerMoved(e);
    }

    double _scaleFactor = 1d;
    public double ScaleFactor {
        get => _scaleFactor;
        set {
            _scaleFactor = value;
            _scaleMatrix = Matrix.Identity;

            // point = point.WithY((Bounds.Height - point.Y)/_scaleFactor);
            Console.WriteLine($"Scale: {_scaleFactor}");
    
            var m = Matrix.Identity;
            m = m.Append(Matrix.CreateScale(1d, -1d));
            m = m.Append(Matrix.CreateTranslation(0, Bounds.Height));
            m = m.Append(_translationMatrix);
            var anchor = m.Transform(_curPosition);
            
            // anchor = new Avalonia.Point(0, 0);
    
            // _scaleMatrix = _scaleMatrix.Append(
            //     Matrix.CreateScale(_scaleFactor, _scaleFactor)
            // );
            // _scaleMatrix = _scaleMatrix.Append(
            //     Matrix.CreateTranslation((1-_scaleFactor) * anchor)
            // );
            _scaleMatrix = _scaleMatrix.Append(
                Matrix.CreateScale(_scaleFactor, _scaleFactor)
            );
            _scaleMatrix = _scaleMatrix.Append(
                Matrix.CreateTranslation(anchor*(1-_scaleFactor))
            ); 
            
            InvalidateVisual();
        }
    }
    Matrix _scaleMatrix = Matrix.Identity;
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var v = e.Delta;

        if(v.Y > 0)
        {
            ScaleFactor *= 1.2;
        } else
        {
            ScaleFactor /= 1.2;
        }

        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    public DrawingCanvas()
    {
        Translation = new(0, 0);
        
        AffectsRender<DrawingCanvas>(
            BackgroundProperty,
             LineStyleProperty,
                TablesProperty
        );
    }

    /// <summary>
    /// Найти точки, которые влияют на отрисовку кривой:
    /// точки которые попадают на экран по оси X
    /// плюс 1 с каждой стороны
    /// </summary>
    /// <returns></returns>
    (int x0, int x1) FindNeedToDraw(ObservableCollection<Point> serie)
    {
        var left = -Translation.X;
        var right = -Translation.X + Bounds.Right;
        int x0 = 0;
        for (int i0 = 0; i0 < serie.Count; i0++)
        {
            if (serie[i0].X >= left)
            {
                x0 = i0 - 1;
                break;
            }
        }
        if (x0 < 0)
        {
            x0 = 0;
        }
        int x1 = serie.Count - 1;
        for (int i1 = x0; i1 < serie.Count; i1++)
        {
            if (serie[i1].X >= right)
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

        var m = Matrix.Identity;
        m = m.Append(Matrix.CreateScale(1d, -1d));
        m = m.Append(Matrix.CreateTranslation(0, Bounds.Height));
        m = m.Append(_translationMatrix);
        m = m.Append(_scaleMatrix);
        context.PushTransform(m);
        foreach (var table in Tables)
        {
            var points = table.Points;
            var br = new SolidColorBrush(table.Color);
            var pen = new Pen(br, 10, lineCap: PenLineCap.Round);

            switch (LineStyle)
            {
                case ELineStyle.PolyLine:
                    context.DrawGeometry(
                        br, pen,
                        new PolylineGeometry(points.Select(x => x.AsAvalonian()), false)
                    );
                    break;
                case ELineStyle.CubicBezier:
                    var pathFigures = new PathFigures();
                    var pathFigure = new PathFigure()
                    {
                        StartPoint = points[0].AsAvalonian(),
                        IsFilled = false,
                        IsClosed = false
                    };
                    var segments = new PathSegments();
                    pathFigure.Segments = segments;
                    pathFigures.Add(pathFigure);

                    for (int i = 0; i < points.Count-1; i++)
                    {
                        var hx = (points[i + 1].X - points[i].X) / 2.0;
                        var hy = (points[i + 1].Y - points[i].Y) / 2.0;
                        segments.Add(new BezierSegment
                        {
                            Point1 = points[i].AsAvalonian() + new Avalonia.Point(hx, hy / 2),
                            Point2 = points[i + 1].AsAvalonian() - new Avalonia.Point(hx, hy / 2),
                            Point3 = points[i + 1].AsAvalonian()
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

        var br2 = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        var pen2 = new Pen(br2, 10, lineCap: PenLineCap.Round);
        var ptr = m.Invert().Transform(_curPosition);
        context.DrawLine(pen2, ptr, ptr + new Avalonia.Point(10, 10));

        base.Render(context);
        Console.Write(".");
    }
}
