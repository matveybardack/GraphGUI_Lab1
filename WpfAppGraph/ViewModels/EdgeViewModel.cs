using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.ViewModels
{
    public partial class EdgeViewModel : ObservableObject
    {
        public VertexViewModel Source { get; }
        public VertexViewModel Target { get; }

        [ObservableProperty]
        private double _weight;

        // Если меняется направленность, нужно перерисовать стрелку
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArrowGeometry))]
        private bool _isDirected;

        [ObservableProperty]
        private EdgeType _type;

        // --- Координаты линии (статичные, вычисляются один раз) ---
        public double X1 => Source.CenterX;
        public double Y1 => Source.CenterY;
        public double X2 => Target.CenterX;
        public double Y2 => Target.CenterY;

        // Координаты для текста (середина)
        public double LabelX => (X1 + X2) / 2 - 10;
        public double LabelY => (Y1 + Y2) / 2 - 10;

        // Геометрия стрелки (зависит от IsDirected)
        public Geometry ArrowGeometry => IsDirected ? _cachedArrowGeometry : Geometry.Empty;

        private readonly Geometry _cachedArrowGeometry;

        public EdgeViewModel(VertexViewModel source, VertexViewModel target, double weight, bool isDirected)
        {
            Source = source;
            Target = target;
            Weight = weight;
            _isDirected = isDirected; // Присваиваем поле напрямую, чтобы не триггерить PropertyChanged в конструкторе лишний раз
            Type = EdgeType.Default;

            // 1. Предварительно вычисляем геометрию стрелки (она не изменится, т.к. вершины не двигаются)
            _cachedArrowGeometry = CalculateArrowGeometry();
        }

        private Geometry CalculateArrowGeometry()
        {
            double dx = X2 - X1;
            double dy = Y2 - Y1;
            double length = Math.Sqrt(dx * dx + dy * dy);

            // Если вершины совпадают или слишком близко (внутри радиуса), стрелку не рисуем
            if (length <= 20) return Geometry.Empty;

            double ux = dx / length;
            double uy = dy / length;

            // Отступаем 20 пикселей от центра целевой вершины (радиус круга)
            double endX = X2 - ux * 20;
            double endY = Y2 - uy * 20;

            double arrowLen = 10;
            double arrowWidth = 5;

            double px = -uy;
            double py = ux;

            Point p1 = new Point(endX, endY);
            Point p2 = new Point(endX - ux * arrowLen + px * arrowWidth, endY - uy * arrowLen + py * arrowWidth);
            Point p3 = new Point(endX - ux * arrowLen - px * arrowWidth, endY - uy * arrowLen - py * arrowWidth);

            StreamGeometry geom = new StreamGeometry();
            using (StreamGeometryContext ctx = geom.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
            }
            geom.Freeze();
            return geom;
        }
    }
}
