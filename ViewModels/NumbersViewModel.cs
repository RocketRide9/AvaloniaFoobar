using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Foobar.Models;
using System.IO;
using System.Linq;

namespace Foobar.ViewModels
{
    // Instead of implementing "INotifyPropertyChanged" on our own we use "ReactiveObject" as 
    // our base class. Read more about it here: https://www.reactiveui.net
    public class NumbersViewModel : ReactiveObject
    {
        public ObservableCollection<Point> Points { get; }

        public event Action PointsChanged;

        public NumbersViewModel()
        {
            var points = new List<Point>
            {
                new(1, 3),
                new(10, 30),
                new(600, 400)
            };
            Points = new(points);
        }

        public void ImportFromReader(StreamReader reader)
        {
            var file = reader.ReadToEnd().Trim();
            var pointsByLines = file.Split("\n").Select(val => val.Split(" "));

            Points.Clear();
            foreach (var coordPair in pointsByLines)
            {
                if (coordPair.Length != 2)
                {
                    throw new Exception($"Invalid line in imported file: {coordPair}");
                }
                var point = new Point(
                    double.Parse(coordPair[0]),
                    double.Parse(coordPair[1])
                );

                Points.Add(point);
            }
        }

        public void ExportToWriter(StreamWriter writer)
        {
            foreach (var point in Points)
            {
                writer.WriteLine($"{point.X} {point.Y}");
            }
        }
    }
}
