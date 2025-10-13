using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using Foobar.Models;
using Foobar.ViewModels;
using ReactiveUI;

namespace Foobar.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        Activated += (_, _) =>
        {
            var vm = ViewModel;
            vm.ActiveTable.Points.CollectionChanged += (_, _) => MyCanvas.InvalidateVisual();
            MyGrid.CellEditEnded += (_, _) => MyCanvas.InvalidateVisual();
            vm.Tables.CollectionChanged += (_, _) => MyCanvas.InvalidateVisual();

            vm.ActivateTableCommand.Subscribe(
                _ => MyGrid.ItemsSource = vm.ActiveTable.Points
            );
        };

    }

    public void OnAddPoint(object sender, RoutedEventArgs args)
    {
        var tab = ViewModel.ActiveTable;

        var last = tab.Points.Last();
        tab.Points.Add(
            new Point(last.X + 10, last.Y)
        );
    }

    public void OnRemovePoint(object sender, RoutedEventArgs args)
    {
        var tab = ViewModel.ActiveTable;

        tab.Points.RemoveAt(tab.Points.Count - 1);
    }

    public void OnAddTable(object sender, RoutedEventArgs args)
    {
        var vm = ViewModel;

        vm.Tables.Add(TableModel.NewDefault(new(255, 255, 255, 0)));
    }
    
    public void OnResetView(object sender, RoutedEventArgs args)
    {
        var vm = ViewModel;

        MyCanvas.Translation = new(0, 0);
        MyCanvas.ScaleFactor = 1;
    }

    public void OnRemoveTable(object sender, RoutedEventArgs args)
    {
        var vm = ViewModel;
        var ltab = vm.Tables[^1];
        if (ltab == vm.ActiveTable)
        {
            vm.ActiveTable = vm.Tables[^2];
        }

        vm.Tables.RemoveAt(vm.Tables.Count - 1);
    }

    public async void OnImport(object sender, RoutedEventArgs args)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = GetTopLevel(this);

        var cwd = Environment.CurrentDirectory;
        var start = await topLevel.StorageProvider.TryGetFolderFromPathAsync(cwd);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            SuggestedStartLocation = start
        });

        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            var file = files.Single();
            await using var stream = await file.OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            ViewModel.ActiveTable.ImportFromReader(file.Path.AbsolutePath, streamReader);
        }
    }
    public async void OnExport(object sender, RoutedEventArgs args)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = GetTopLevel(this);

        var cwd = Environment.CurrentDirectory;
        var start = await topLevel.StorageProvider.TryGetFolderFromPathAsync(cwd);

        // Start async operation to open the dialog.
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Open Text File",
            SuggestedFileName = "ExportedPoints",
            SuggestedStartLocation = start
        });

        if (file != null)
        {
            // Open reading stream from the first file.
            await using var stream = await file.OpenWriteAsync();
            using var streamWriter = new StreamWriter(stream);
            ViewModel.ActiveTable.ExportToWriter(streamWriter);
        }
    }
}

public class ColorToBrushExceptionConverter : IValueConverter
{
    public static readonly ColorToBrushExceptionConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Color item) return new SolidColorBrush(item);

        throw new UnreachableException();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is SolidColorBrush brush) return brush.Color;

        throw new UnreachableException();
    }

}
