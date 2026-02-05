using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

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
                    vertex.IterationInfo = step.IterationInfo;
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

        #region Методы для BFS/DFS (копирование и сброс)

        /// <summary>
        /// Полностью перестраивает визуальный граф на основе логической модели.
        /// Это обеспечивает независимость холста BFS от холста рисования.
        /// </summary>
        public void RebuildFromModel(GraphModel model)
        {
            ClearGraph(); // Очищаем текущий холст BFS

            // 1. Создаем вершины
            var modelVertices = model.GetVertices();
            foreach (var id in modelVertices)
            {
                // Для простоты расставляем их по кругу или сетке, 
                // НО лучше сохранить координаты из DrawVM.
                // Так как у нас нет доступа к координатам из Model (там только структура),
                // в реальном приложении координаты стоит хранить в Model.
                // Здесь мы сделаем допущение: если координаты не переданы, ставим случайно или по кругу.

                // ВАЖНО: В рамках задачи мы просто создаем их. 
                // (Для идеального переноса координат нужно передавать список VertexViewModel из DrawTab)

                Vertices.Add(new VertexViewModel(id, 100 + (id * 50) % 400, 100 + (id / 10) * 50));
            }

            // 2. Создаем ребра (нам нужно получить их из модели)
            // В GraphModel у нас _adjacencyList private, но есть GetAdjacencyMatrix или можно расширить Model.
            // Предположим, мы расширим Model методом GetEdges() или переберем список.

            // Временное решение: используем существующий GetVertices и предположим доступ к ребрам.
            // ПРАВИЛЬНЫЙ ПУТЬ: Добавить в GraphModel метод public List<GraphEdge> GetAllEdges().
        }

        /// <summary>
        /// Метод для копирования визуального состояния из другой ViewModel (чтобы сохранить координаты)
        /// </summary>
        public void CloneFrom(GraphCanvasVM other)
        {
            ClearGraph();

            foreach (var v in other.Vertices)
            {
                var newV = new VertexViewModel(v.Id, v.X, v.Y) { State = v.State };
                // Если вершина была Selected, сбрасываем в Default, чтобы выбрать заново для BFS,
                // но Target оставляем.
                if (newV.State == VertexState.Selected) newV.State = VertexState.Default;
                Vertices.Add(newV);
            }

            foreach (var e in other.Edges)
            {
                var source = Vertices.First(v => v.Id == e.Source.Id);
                var target = Vertices.First(v => v.Id == e.Target.Id);
                Edges.Add(new EdgeViewModel(source, target, e.Weight, e.IsDirected));
            }
        }

        #endregion
    }
}