using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Foobar.Models;
using System.IO;
using System.Linq;
using Avalonia;

namespace Foobar.Models
{
    public class Activatable<T>
    {
        public bool Active { get; set; }
        public required T Table { get; set;}
    }
    public partial class TableModel : ReactiveObject
    {
        public ObservableCollection<Point> Points { get; private set; } = [];
        public string Name { get; private set; } = "";
        
        [ReactiveUI.SourceGenerators.Reactive]
        private Color _color;

        private TableModel()
        {
            
        }

        public static TableModel NewDefault (Color color)
        {
            var res = new TableModel
            {
                Name = "Default",
                Color = color
            };

            var points = new List<Point>
            {
                new(1, 3),
                new(10, 30),
                new(600, 400)
            };
            res.Points = new(points);

            return res;
        }

        public void ImportFromReader(string filePath, StreamReader reader)
        {
            var file = reader.ReadToEnd().Trim();
            var pointsByLines = file.Split("\n").Select(val => val.Split(" "));

            Name = Path.GetFileName(filePath);
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
