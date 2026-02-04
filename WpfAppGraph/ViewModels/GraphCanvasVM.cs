using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphCanvasVM : ObservableObject
    {
        // Коллекции для привязки к ItemsControl в XAML
        public ObservableCollection<VertexViewModel> Vertices { get; } = new ObservableCollection<VertexViewModel>();
        public ObservableCollection<EdgeViewModel> Edges { get; } = new ObservableCollection<EdgeViewModel>();

        // События для уведомления родительской ViewModel (DrawGraphTabViewModel)
        // Родитель решит, что делать: соединять вершины, удалять или выбирать цель
        public event Action<VertexViewModel>? VertexClicked;
        public event Action<Point>? CanvasClicked;

        /// <summary>
        /// Команда, вызываемая при клике на КНОПКУ внутри шаблона вершины.
        /// Привязана в XAML: Command="{Binding DataContext.VertexClickCommand ...}"
        /// </summary>
        [RelayCommand]
        private void VertexClick(VertexViewModel vertex)
        {
            VertexClicked?.Invoke(vertex);
        }

        /// <summary>
        /// Метод, вызываемый из Code-Behind (xaml.cs) при клике на пустое место холста.
        /// </summary>
        public void OnCanvasClick(Point position)
        {
            CanvasClicked?.Invoke(position);
        }

        #region Методы изменения графа (вызываются из DrawGraphTabViewModel)

        public VertexViewModel AddVertex(double x, double y)
        {
            // Генерация ID: ищем максимальный + 1, или берем 1
            int newId = Vertices.Count > 0 ? Vertices.Max(v => v.Id) + 1 : 1;

            // Смещаем на -20, чтобы центр круга (40x40) был ровно под курсором мыши
            var vertex = new VertexViewModel(newId, x - 20, y - 20);
            Vertices.Add(vertex);
            return vertex;
        }

        public void AddEdge(VertexViewModel from, VertexViewModel to, double weight, bool isDirected)
        {
            // Проверка на дубликаты (если ребро уже есть — обновляем или игнорируем)
            var existing = Edges.FirstOrDefault(e => e.Source == from && e.Target == to);
            if (existing != null)
            {
                // Можно обновить вес, если ребро уже существует
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
        /// Сбрасывает только визуальные состояния (цвета), но оставляет структуру графа.
        /// Нужно перед запуском нового алгоритма.
        /// </summary>
        public void ResetVisuals()
        {
            foreach (var v in Vertices)
            {
                // Не сбрасываем Target, если пользователь его задал, или сбрасываем всё в Default?
                // Обычно Target оставляют, а visited/active сбрасывают.
                if (v.State != VertexState.Target)
                {
                    v.State = VertexState.Default;
                }
                v.IterationInfo = string.Empty;
            }

            foreach (var e in Edges)
            {
                e.Type = EdgeType.Default;
            }
        }

        #endregion

        #region Методы для визуализации алгоритмов

        /// <summary>
        /// Применяет шаг алгоритма к визуальным элементам
        /// </summary>
        public void ApplyAlgorithmStep(AlgorithmStep step)
        {
            // 1. Обновление состояния вершины
            if (step.VertexId != -1 && step.NewVertexState.HasValue)
            {
                var vertex = Vertices.FirstOrDefault(v => v.Id == step.VertexId);
                if (vertex != null)
                {
                    // Если вершина была целевой, мы можем захотеть сохранить её статус "Target", 
                    // но для анимации алгоритма важнее показать, что она сейчас Visited.
                    // Тут логика зависит от предпочтений. Пока просто красим.
                    vertex.State = step.NewVertexState.Value;
                }
            }

            // 2. Обновление состояния ребра
            if (step.EdgeFromId != -1 && step.EdgeToId != -1 && step.NewEdgeType.HasValue)
            {
                // Ищем ребро. Учитываем, что EdgeViewModel хранит объекты, а шаг — ID.
                var edge = Edges.FirstOrDefault(e =>
                    e.Source.Id == step.EdgeFromId &&
                    e.Target.Id == step.EdgeToId);

                // Если граф неориентированный, ребро может быть записано как A->B, а алгоритм идет B->A.
                // Но у нас ViewModel ребра хранит Source/Target жестко.
                // Если не нашли прямое, ищем обратное (для неориентированных графов визуально это одно ребро)
                if (edge == null)
                {
                    edge = Edges.FirstOrDefault(e =>
                       !e.IsDirected && // Только если ребро неориентированное
                       e.Source.Id == step.EdgeToId &&
                       e.Target.Id == step.EdgeFromId);
                }

                if (edge != null)
                {
                    edge.Type = step.NewEdgeType.Value;
                }
            }
        }

        #endregion
    }
}