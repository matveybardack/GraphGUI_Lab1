using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Windows;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.ViewModels
{
    public partial class DrawGraphVM : ObservableObject
    {
        // Визуальная модель холста (содержит списки вершин и ребер для UI)
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        // Логическая модель графа (содержит структуру для алгоритмов)
        private readonly GraphModel _graphModel = new GraphModel();

        // --- Состояние интерфейса ---

        [ObservableProperty]
        private GraphTool _currentTool = GraphTool.AddVertex; // По умолчанию рисуем вершины

        [ObservableProperty]
        private object _activeDialog; // Если не null, отображается модальное окно

        [ObservableProperty]
        private int _vertexCount;

        [ObservableProperty]
        private int _edgeCount;

        // Временное хранение первой вершины при создании ребра
        private VertexViewModel _firstSelectedVertex;

        public DrawGraphVM()
        {
            // Подписка на события от холста (клики, которые пробросил GraphCanvasVM)
            GraphCanvas.CanvasClicked += OnCanvasClicked;
            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        // --- Обработка событий холста ---

        private void OnCanvasClicked(Point point)
        {
            // Если у нас выбрано создание ребра и мы кликнули мимо вершины -> сбрасываем выделение
            if (CurrentTool == GraphTool.AddEdge && _firstSelectedVertex != null)
            {
                ResetSelection();
                return;
            }

            if (CurrentTool == GraphTool.AddVertex)
            {
                // 1. Добавляем в Визуальную часть
                var newVertexVm = GraphCanvas.AddVertex(point.X, point.Y);

                // 2. Добавляем в Логическую часть
                _graphModel.AddVertex(newVertexVm.Id);

                UpdateStats();
            }
        }

        private void OnVertexClicked(VertexViewModel vertex)
        {
            switch (CurrentTool)
            {
                case GraphTool.AddVertex:
                    // При режиме добавления вершин клик по существующей ничего не делает
                    break;

                case GraphTool.SetTarget:
                    HandleSetTarget(vertex);
                    break;

                case GraphTool.AddEdge:
                    HandleAddEdge(vertex);
                    break;
            }
        }

        // --- Логика инструментов ---

        private void HandleSetTarget(VertexViewModel vertex)
        {
            // Сбрасываем предыдущую цель (если была)
            var oldTarget = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            if (oldTarget != null) oldTarget.State = VertexState.Default;

            // Если кликнули по той же самой - просто снимаем выделение
            if (oldTarget == vertex) return;

            // Назначаем новую
            vertex.State = VertexState.Target;
            // В модели графа хранить "Target" не обязательно, это параметр алгоритма, 
            // который будет передан при запуске DFS/BFS.
        }

        private void HandleAddEdge(VertexViewModel clickedVertex)
        {
            // 1. Если это первый клик (начало ребра)
            if (_firstSelectedVertex == null)
            {
                _firstSelectedVertex = clickedVertex;
                _firstSelectedVertex.State = VertexState.Selected; // Подсвечиваем оранжевым
            }
            // 2. Если кликнули по той же самой вершине (отмена или петля)
            else if (_firstSelectedVertex == clickedVertex)
            {
                // Допустим, мы пока запретим петли или просто сбросим выделение
                ResetSelection();
            }
            // 3. Если это второй клик (конец ребра)
            else
            {
                // Открываем диалог настройки ребра
                OpenEdgeDialog(_firstSelectedVertex, clickedVertex);
            }
        }

        private void OpenEdgeDialog(VertexViewModel source, VertexViewModel target)
        {
            // Создаем VM для диалога
            var dialogVm = new EdgeDialogVM(
                onConfirm: (weight, isDirected) =>
                {
                    CreateEdge(source, target, weight, isDirected);
                    CloseDialog();
                },
                onCancel: () =>
                {
                    CloseDialog();
                    ResetSelection();
                }
            );

            ActiveDialog = dialogVm;
        }

        private void CreateEdge(VertexViewModel source, VertexViewModel target, double weight, bool isDirected)
        {
            // 1. Визуальное добавление
            GraphCanvas.AddEdge(source, target, weight, isDirected);

            // 2. Логическое добавление
            _graphModel.AddEdge(source.Id, target.Id, weight, isDirected);

            UpdateStats();
            ResetSelection();
        }

        private void CloseDialog()
        {
            ActiveDialog = null;
        }

        private void ResetSelection()
        {
            if (_firstSelectedVertex != null)
            {
                // Если она была Target, возвращаем Target, иначе Default
                // (упрощенно возвращаем Default, если она не Target)
                if (_firstSelectedVertex.State != VertexState.Target)
                    _firstSelectedVertex.State = VertexState.Default;

                _firstSelectedVertex = null;
            }
        }

        [RelayCommand]
        private void ResetGraph()
        {
            GraphCanvas.ClearGraph();
            _graphModel.Clear();
            ResetSelection();
            UpdateStats();
        }

        private void UpdateStats()
        {
            VertexCount = GraphCanvas.Vertices.Count;
            EdgeCount = GraphCanvas.Edges.Count;
        }

        // Метод для получения модели (понадобится другим вкладкам/VM)
        public GraphModel GetGraphModel() => _graphModel;
    }
}