using System.Collections.Generic;
using Foobar.Views;
using ReactiveUI;

namespace Foobar.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public NumbersViewModel NumbersViewModel { get; } = new();

        public MainWindowViewModel()
        {
            StyleChoices = [
                new LineStyleChoice("Polyline", DrawingCanvas.ELineStyle.PolyLine),
                new LineStyleChoice("Cubic Bezier", DrawingCanvas.ELineStyle.CubicBezier),
            ];

            _lineStyleChoice = StyleChoices[0];
            SelectedStyle = _lineStyleChoice;
        }

        public record LineStyleChoice(string Label, DrawingCanvas.ELineStyle Style);

        LineStyleChoice _lineStyleChoice;
        public LineStyleChoice SelectedStyle
        {
            get => _lineStyleChoice;
            set => this.RaiseAndSetIfChanged(ref _lineStyleChoice, value);
        }

        public List<LineStyleChoice> StyleChoices { get; set; }
    }
}
