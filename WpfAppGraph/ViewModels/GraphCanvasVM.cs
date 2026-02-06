using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfAppGraph.Configs;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphCanvasVM : ObservableObject
    {
        public ObservableCollection<VertexViewModel> Vertices { get; } = new ObservableCollection<VertexViewModel>();
        public ObservableCollection<EdgeViewModel> Edges { get; } = new ObservableCollection<EdgeViewModel>();

        // Сообщение между вкладкой и холстом
        public event Action<VertexViewModel>? VertexClicked;
        public event Action<Point>? CanvasClicked;

        [RelayCommand]
        private void VertexClick(VertexViewModel vertex)
        {
            VertexClicked?.Invoke(vertex);
        }

        public void OnCanvasClick(Point position)
        {
            CanvasClicked?.Invoke(position);
        }

        #region Методы изменения графа (только для DrawGraphTab)

        public VertexViewModel AddVertex(double x, double y)
        {
            int newId = Vertices.Count > 0 ? Vertices.Max(v => v.Id) + 1 : 1;

            var vertex = new VertexViewModel(newId, x - Parameters.VertexRadius, y - Parameters.VertexRadius);
            Vertices.Add(vertex);
            return vertex;
        }

        public void AddEdge(VertexViewModel from, VertexViewModel to, double weight, bool isDirected)
        {
            // Проверка на дубликаты (изменение параметров)
            var existing = Edges.FirstOrDefault(e => e.Source == from && e.Target == to);
            if (existing != null)
            {
                existing.Weight = weight;
                existing.IsDirected = isDirected;
                return;
            }

            var edge = new EdgeViewModel(from, to, weight, isDirected);
            Edges.Add(edge);
        }

        public void ClearGraph()
        {
            Edges.Clear();
            Vertices.Clear();
        }

        /// <summary>
        /// Сброс визуальных параметров (кроме цели)
        /// </summary>
        public void ResetVisuals()
        {
            foreach (var v in Vertices)
            {
                if (v.State != VertexState.Target)
                    v.State = VertexState.Default;

                v.IterationInfo = string.Empty;
            }

            foreach (var e in Edges)
                e.Type = EdgeType.Default;
        }

        #endregion

        #region Методы для визуализации алгоритмов

        /// <summary>
        /// Один шаг алгоритма к визуальным элементам
        /// </summary>
        public void ApplyAlgorithmStep(AlgorithmStep step)
        {
            // Обновление состояния вершины
            if (step.VertexId != -1 && step.NewVertexState.HasValue)
            {
                var vertex = Vertices.FirstOrDefault(v => v.Id == step.VertexId);
                if (vertex != null)
                {
                    vertex.State = step.NewVertexState.Value;
                    vertex.IterationInfo = step.IterationInfo;
                }
            }

            // Обновление состояния ребра
            if (step.EdgeFromId != -1 && step.EdgeToId != -1 && step.NewEdgeType.HasValue)
            {
                // Проверка в одну сторону
                var edge = Edges.FirstOrDefault(e =>
                    e.Source.Id == step.EdgeFromId &&
                    e.Target.Id == step.EdgeToId);

                // Проверка в другую сторону (только для неориентированных ребер)
                if (edge == null)
                {
                    edge = Edges.FirstOrDefault(e =>
                       !e.IsDirected &&
                       e.Source.Id == step.EdgeToId &&
                       e.Target.Id == step.EdgeFromId);
                }

                if (edge != null)
                    edge.Type = step.NewEdgeType.Value;
            }
        }

        #endregion

        /// <summary>
        /// Метод для копирования визуального состояния из другой ViewModel (чтобы сохранить координаты)
        /// </summary>
        public void CloneFrom(GraphCanvasVM other)
        {
            ClearGraph();

            foreach (var v in other.Vertices)
            {
                var newV = new VertexViewModel(v.Id, v.X, v.Y) { State = v.State };
                Vertices.Add(newV);
            }

            foreach (var e in other.Edges)
            {
                var source = Vertices.First(v => v.Id == e.Source.Id);
                var target = Vertices.First(v => v.Id == e.Target.Id);
                Edges.Add(new EdgeViewModel(source, target, e.Weight, e.IsDirected));
            }
        }
    }
}