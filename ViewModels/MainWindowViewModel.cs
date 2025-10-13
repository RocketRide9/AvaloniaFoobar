using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Media;
using Foobar.Views;
using Foobar.Models;
using ReactiveUI;
using System.Linq;


namespace Foobar.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TableModel> Tables { get; } = [];
    public TableModel ActiveTable { get; set; }

    public MainWindowViewModel()
    {
        StyleChoices = [
            new LineStyleChoice("Polyline", DrawingCanvas.ELineStyle.PolyLine),
            new LineStyleChoice("Cubic Bezier", DrawingCanvas.ELineStyle.CubicBezier),
        ];

        _lineStyleChoice = StyleChoices[0];
        SelectedStyle = _lineStyleChoice;

        ActiveTable = TableModel.NewDefault(new Color(255, 255, 0, 0));
        Tables.Add(ActiveTable);

        ActivateTableCommand = ReactiveCommand.Create<TableModel>(
            val => {
                ActiveTable = val;
            }
        );
    }

    public ReactiveCommand<TableModel, Unit> ActivateTableCommand { get; }

    public record LineStyleChoice(string Label, DrawingCanvas.ELineStyle Style);

    LineStyleChoice _lineStyleChoice;
    public LineStyleChoice SelectedStyle
    {
        get => _lineStyleChoice;
        set => this.RaiseAndSetIfChanged(ref _lineStyleChoice, value);
    }

    public List<LineStyleChoice> StyleChoices { get; set; }
}

