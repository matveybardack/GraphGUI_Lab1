using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; 
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphDFSVM : ObservableObject
    {
        // Холст для этой вкладки
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        // --- Настройки алгоритма ---

        [ObservableProperty]
        private DfsAlgorithmType _algorithmType = DfsAlgorithmType.Iterative;

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
        [NotifyCanExecuteChangedFor(nameof(StartDfsCommand))]
        [NotifyCanExecuteChangedFor(nameof(SyncGraphCommand))]
        private bool _isAnimating;

        // Выбранная стартовая вершина
        private VertexViewModel? _startVertex;

        public GraphDFSVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;

            // Подписываемся на события холста для выбора стартовой вершины
            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        /// <summary>
        /// Обработчик клика по вершине (выбор старта).
        /// </summary>
        private void OnVertexClicked(VertexViewModel vertex)
        {
            if (IsAnimating) return;

            // Снимаем выделение с предыдущей
            if (_startVertex != null && _startVertex.State == VertexState.Selected)
            {
                _startVertex.State = VertexState.Default;
            }

            // Если кликнули ту же самую -> отмена выбора
            if (_startVertex == vertex)
            {
                _startVertex = null;
                ResultStatus = "Стартовая вершина не выбрана";
            }
            else
            {
                _startVertex = vertex;
                // Не перекрашиваем Target
                if (_startVertex.State != VertexState.Target)
                {
                    _startVertex.State = VertexState.Selected;
                }
                ResultStatus = $"Выбрана стартовая вершина: {vertex.Id}";
            }
        }

        /// <summary>
        /// Копирует граф с вкладки рисования.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            // Сохраняем ID, чтобы попытаться восстановить выбор после перерисовки
            int? prevStartId = _startVertex?.Id;

            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

            // Восстанавливаем ссылку на старт
            if (prevStartId.HasValue)
            {
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.Id == prevStartId.Value);
                if (_startVertex != null && _startVertex.State != VertexState.Target)
                {
                    _startVertex.State = VertexState.Selected;
                    ResultStatus = $"Выбрана стартовая вершина: {_startVertex.Id}";
                }
            }
            else
            {
                ResultStatus = "Граф синхронизирован. Выберите вершину.";
            }

            // Сбрасываем старые результаты
            IsResultAvailable = false;
            ParenthesisStructure = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartDfs()
        {
            // 1. Авто-синхронизация перед запуском (можно убрать, если хотите только ручную)
            SyncGraph();

            if (GraphCanvas.Vertices.Count == 0)
            {
                ResultStatus = "Граф пуст. Нарисуйте граф на первой вкладке.";
                return;
            }

            // 2. Поиск цели (Target)
            var targetVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            int? targetId = targetVertex?.Id;

            // Если старт не выбран, ищем Selected, или берем первую попавшуюся (для обхода леса)
            // Но лучше требовать старт, чтобы задать порядок обхода
            if (_startVertex == null)
            {
                // Попробуем найти selected (вдруг SyncGraph восстановил)
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Selected);
            }

            // Если все равно null, берем минимальный ID как точку входа (или выводим ошибку)
            // Для UX лучше начать с минимального ID, если пользователь ничего не выбрал.
            int? startId = _startVertex?.Id;

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск...";

            // Сброс цветов (оставляем структуру)
            GraphCanvas.ResetVisuals();
            // Возвращаем подсветку старта и цели
            if (_startVertex != null) _startVertex.State = VertexState.Selected;
            if (targetVertex != null) targetVertex.State = VertexState.Target;

            // 3. Выбор алгоритма и получение шагов
            var resultData = new DfsResult();
            IEnumerable<AlgorithmStep> steps;

            if (AlgorithmType == DfsAlgorithmType.Iterative)
            {
                steps = _graphModel.RunDfsIterative(startId, targetId, resultData);
            }
            else
            {
                steps = _graphModel.RunDfsRecursive(startId, targetId, resultData);
            }

            // 4. Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);

                // Обновляем структуру скобок в реальном времени? 
                // В Model StringBuilder собирается локально, но если бы мы передавали 
                // частичные строки в step, можно было бы обновлять тут.
                // Сейчас обновим в конце.

                await Task.Delay(300); // Задержка
            }

            // 5. Вывод результатов
            PathLength = resultData.PathLength;
            ParenthesisStructure = resultData.ParenthesisStructure;

            if (resultData.IsTargetFound)
            {
                PathString = string.Join(" -> ", resultData.Path);
                ResultStatus = "Цель найдена!";
                HighlightPath(resultData.Path);
            }
            else
            {
                PathString = "Путь не найден";
                ResultStatus = targetId.HasValue ? "Цель недостижима" : "Обход завершен";
            }

            IsResultAvailable = true;
            IsAnimating = false;
        }

        private void HighlightPath(List<int> path)
        {
            // Утолщаем ребра пути
            // (Логика аналогична BFS, так как структура визуализации одна)
            for (int i = 0; i < path.Count - 1; i++)
            {
                int u = path[i];
                int v = path[i + 1];

                var edge = GraphCanvas.Edges.FirstOrDefault(e =>
                    (e.Source.Id == u && e.Target.Id == v) ||
                    (!e.IsDirected && e.Source.Id == v && e.Target.Id == u));

                // Можно было бы покрасить в специальный цвет, но пока оставляем как есть,
                // так как в TreeEdge они уже покрашены, а путь просто показываем текстом.
            }
        }

        private bool CanInteract() => !IsAnimating;
    }
}
