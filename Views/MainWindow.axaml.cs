using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
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
            var numsVm = ViewModel.NumbersViewModel;
            numsVm.Points.CollectionChanged += (_, _) => MyCanvas.InvalidateVisual();
            MyGrid.CellEditEnded += (_, _) => MyCanvas.InvalidateVisual();
        };

    }

    public void OnAddPoint(object sender, RoutedEventArgs args)
    {
        var numsVm = ViewModel.NumbersViewModel;

        var last = numsVm.Points.Last();
        numsVm.Points.Add(
            new Models.Point(last.X + 10, last.Y)
        );
    }

    public void OnRemovePoint(object sender, RoutedEventArgs args)
    {
        var vm = ViewModel;
        var numsVm = vm.NumbersViewModel;

        numsVm.Points.RemoveAt(numsVm.Points.Count - 1);
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
            await using var stream = await files.Single().OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            var ctx = (MainWindowViewModel)DataContext;
            var model = ctx.NumbersViewModel;
            model.ImportFromReader(streamReader);
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
            var ctx = (MainWindowViewModel)DataContext;
            var numsVm = ctx.NumbersViewModel;
            numsVm.ExportToWriter(streamWriter);
        }
    }
}
