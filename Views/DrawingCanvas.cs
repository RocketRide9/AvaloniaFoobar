using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
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
            var anchor = _curPosition/_scaleFactor;
            var scaleChange = value/_scaleFactor;
            _scaleFactor = value;

            Console.WriteLine($"Scale: {_scaleFactor}");
    
            _scaleMatrix = Matrix.CreateScale(_scaleFactor, _scaleFactor);
            Translation += anchor*(1/scaleChange-1);
            
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

    double findClosestStep(int m, double val)
    {
        double[] baseSteps = [2, 5, 10];
        double b = Math.Pow(10, m);
        if (val < 2 * b) {
            return 1 * b;
        }
        if (val < 5 * b) {
            return 2 * b;
        }
        if (val < 10 * b) {
            return 5 * b;
        }
        if (val == 10 * b) {
            return 10 * b;
        }

        Console.WriteLine("val = {0}, b = {1}", val, b);
        throw new ArgumentException();
    }
    
    FormattedText FormatText(string s, double em, IBrush? br)
        => new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Typeface.Default, em, br);
    
    // возвращает
    double Gravitate(double a, double b, double val)
    {
        if (Math.Abs(val-a) < Math.Abs(b-a)) {
            return a; 
        } else {
            return b;
        }
    }
        
    void RenderAxes(DrawingContext context, Matrix transform)
    {
        var m = transform;
        var inv = m.Invert();
        
        // реальные координаты
        var realL = inv.Transform(new(0             , Bounds.Height/2)).WithY(0);
        var realR = inv.Transform(new(Bounds.Width  , Bounds.Height/2)).WithY(0);
        var realT = inv.Transform(new(Bounds.Width/2, 0              )).WithX(0);
        var realB = inv.Transform(new(Bounds.Width/2, Bounds.Height  )).WithX(0);

        Console.WriteLine(realB.ToString());

        // экранные координаты
        var scrL = m.Transform(realL);
        var scrR = m.Transform(realR);
        var scrT = m.Transform(realT);
        var scrB = m.Transform(realB);
        
        var x = new LineGeometry(scrL, scrR);
        var y = new LineGeometry(scrT, scrB);

        var found = this.TryFindResource("ThemeForegroundColor", this.ActualThemeVariant, out var smth);
        var br = new SolidColorBrush(new Color(255, 195, 195, 195));
        var pen = new Pen(br, 3, lineCap: PenLineCap.Round);
        
        var labX = FormatText("X", 24, br);
        var labY = FormatText("Y", 24, br);

        var steps = 10;
        var rawXStep = (realR - realL).X / steps;
        var rawYStep = (realT - realB).Y / steps;
        var kx = (int)Math.Floor(Math.Log10(rawXStep));
        var ky = (int)Math.Floor(Math.Log10(rawYStep));
        var stepX = findClosestStep(kx, rawXStep);
        var stepY = findClosestStep(ky, rawYStep);
        
        var chosen = Math.Max(stepX, stepY);
        var realStep = new Avalonia.Point(chosen, chosen);
        var scrStep = realStep*ScaleFactor;

        var startReal = new Avalonia.Point(
            Math.Floor(realL.X / realStep.X) * realStep.X,
            Math.Floor(realB.Y / realStep.Y) * realStep.Y
        );
        var startScr = m.Transform(startReal);
        for (int i = 0; i <= steps*2; i++)
        {
            var real = startReal + i * realStep;
            var scr = startScr + i * scrStep.WithY(-scrStep.Y);

            var coord = FormatText(real.X.ToString("G6"), 16, br);
            {
                // по x
                Avalonia.Point clamped = new Avalonia.Point(scr.X, Math.Clamp(scrL.Y, 0, scrB.Y));
                context.DrawLine(pen, new(scr.X, clamped.Y-5), new(scr.X, clamped.Y+5));

                if (real.X.ToString("G6") != "0")
                {
                    if (scrL.Y > scrB.Y)
                    {
                        context.DrawText(coord, new(scr.X - coord.Width / 2, clamped.Y - 8 - coord.Height));
                    }
                    else
                    {
                        context.DrawText(coord, new(scr.X - coord.Width / 2, clamped.Y + 8));
                    }
                } else {
                    if (scrL.Y > scrB.Y)
                    {
                        context.DrawText(coord, new(scr.X + 8, clamped.Y - 8 - coord.Height));
                    }
                    else
                    {
                        context.DrawText(coord, new(scr.X + 8, clamped.Y + 8));
                    }
                }
            }
            
            coord = FormatText(real.Y.ToString("G6"), 16, br);
            {
                // по y
                Avalonia.Point clamped = new Avalonia.Point(Math.Clamp(scrB.X, 0, scrR.X), scr.Y);
                context.DrawLine(pen, new(clamped.X-5, scr.Y), new(clamped.X+5, scr.Y));
                
                if (real.Y.ToString("G6") != "0") {
                    if (scrB.X > scrR.X)
                    {
                        context.DrawText(coord, new(clamped.X - 16 - coord.Width, scr.Y - 12));
                    } else {
                        context.DrawText(coord, new(clamped.X + 16, scr.Y - 12));
                    }
                }
            }
        }

        context.DrawGeometry(br, pen, x);
        context.DrawGeometry(br, pen, y);
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
        
        RenderAxes(context, m);
        
        context.PushTransform(m);
        
        foreach (var table in Tables)
        {
            var points = table.Points;
            var br = new SolidColorBrush(table.Color);
            var pen = new Pen(br, 10/ScaleFactor, lineCap: PenLineCap.Round);

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

        base.Render(context);
        Console.Write(".");
    }
}
