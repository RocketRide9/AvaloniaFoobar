using ReactiveUI;

namespace Foobar.Models;

public class Point : ReactiveObject
{
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Avalonia.Point AsAvalonian()
    {
        return new(X, Y);
    }

    double _x;
    double _y;
    public double X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }
    public double Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }
}