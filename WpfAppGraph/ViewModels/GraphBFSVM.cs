using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphBFSVM : ObservableObject
    {
        // Холст для этой вкладки (независимый)
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM; // Ссылка на VM рисования для копирования графа

        // --- Результаты ---

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private double _pathLength;

        [ObservableProperty]
        private string _pathString;

        [ObservableProperty]
        private string _parenthesisStructure;

        [ObservableProperty]
        private bool _isResultAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartBfsCommand))]
        private bool _isAnimating;

        // Выбранная стартовая вершина
        private VertexViewModel? _startVertex;

        public GraphBFSVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;

            // Подписываемся на клики по НАШЕМУ холсту (для выбора старта)
            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        /// <summary>
        /// Обработчик клика по вершине на холсте BFS.
        /// Позволяет выбрать стартовую вершину.
        /// </summary>
        private void OnVertexClicked(VertexViewModel vertex)
        {
            if (IsAnimating) return;

            // Сбрасываем предыдущий старт
            if (_startVertex != null && _startVertex.State == VertexState.Selected)
            {
                _startVertex.State = VertexState.Default;
            }

            // Если кликнули по той же - снимаем выбор
            if (_startVertex == vertex)
            {
                _startVertex = null;
                ResultStatus = "Стартовая вершина не выбрана";
            }
            else
            {
                // Назначаем новую
                _startVertex = vertex;
                // Не перекрашиваем Target, если он был
                if (_startVertex.State != VertexState.Target)
                {
                    _startVertex.State = VertexState.Selected;
                }
                ResultStatus = $"Выбрана стартовая вершина: {vertex.Id}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartBfs))]
        private async Task StartBfs()
        {
            // 1. Синхронизируем граф (копируем актуальное состояние с вкладки рисования)
            // Это гарантирует, что мы работаем с последней версией графа
            SyncGraphFromDrawTab();

            // 2. Проверки
            if (_startVertex == null)
            {
                // Пытаемся найти вершину, которую пользователь мог выбрать до синхронизации,
                // или берем первую попавшуюся, если ничего не выбрано.
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Selected);

                if (_startVertex == null)
                {
                    ResultStatus = "Ошибка: Выберите стартовую вершину (кликните по ней)";
                    return;
                }
            }

            // Ищем цель (она могла быть задана на вкладке рисования и скопирована сюда)
            var targetVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            int? targetId = targetVertex?.Id;

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск...";

            // Сбрасываем визуальные эффекты (цвета, тексты итераций), оставляя структуру
            GraphCanvas.ResetVisuals();
            // Возвращаем подсветку старта и цели после сброса
            _startVertex?.State = VertexState.Selected;
            targetVertex?.State = VertexState.Target;

            // 3. Запуск алгоритма в модели
            var resultData = new BfsResult();
            var steps = _graphModel.RunBfs(_startVertex.Id, targetId, resultData);

            // 4. Анимация (перебираем шаги)
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                // Задержка для анимации (можно вынести скорость в настройки)
                await Task.Delay(300);
            }

            // 5. Вывод результатов
            PathLength = resultData.PathLength;
            ParenthesisStructure = resultData.ParenthesisStructure;

            if (resultData.IsTargetFound)
            {
                PathString = string.Join(" -> ", resultData.Path);
                ResultStatus = "Цель найдена!";

                // Подсветим путь
                HighlightPath(resultData.Path);
            }
            else
            {
                PathString = "Путь не найден";
                ResultStatus = targetId.HasValue ? "Цель недостижима" : "Обход завершен (цель не была задана)";
            }

            IsResultAvailable = true;
            IsAnimating = false;
        }

        private bool CanStartBfs() => !IsAnimating;

        /// <summary>
        /// Копирует граф из вкладки рисования на вкладку BFS.
        /// </summary>
        [RelayCommand]
        public void SyncGraphFromDrawTab()
        {
            if (IsAnimating) return;

            // Сохраняем ID выбранного старта, чтобы попробовать восстановить выбор после перерисовки
            int? prevStartId = _startVertex?.Id;

            // Используем метод клонирования в CanvasVM
            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

            // Восстанавливаем ссылку на объект старта в новой коллекции
            if (prevStartId.HasValue)
            {
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.Id == prevStartId.Value);
                if (_startVertex != null && _startVertex.State != VertexState.Target)
                {
                    _startVertex.State = VertexState.Selected;
                }
            }
        }

        private void HighlightPath(List<int> path)
        {
            // Простой метод подсветки пути (делаем ребра толще или меняем цвет)
            for (int i = 0; i < path.Count - 1; i++)
            {
                int u = path[i];
                int v = path[i + 1];

                var edge = GraphCanvas.Edges.FirstOrDefault(e =>
                    (e.Source.Id == u && e.Target.Id == v) ||
                    (!e.IsDirected && e.Source.Id == v && e.Target.Id == u));

                if (edge != null)
                {
                    // Можно добавить новый тип ребра EdgeType.Path или просто использовать TreeEdge
                    // Пока оставим как есть, так как TreeEdge уже подсвечен.
                }
            }
        }
    }
}