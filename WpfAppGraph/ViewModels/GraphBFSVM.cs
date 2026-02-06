using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;
using WpfAppGraph.Configs;

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

        private VertexViewModel? _startVertex;

        public GraphBFSVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;

            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        /// <summary>
        /// Выбор стартовой вершини
        /// </summary>
        /// <param name="vertex"></param>
        private void OnVertexClicked(VertexViewModel vertex)
        {
            if (IsAnimating) return;

            if (_startVertex != null && _startVertex.State == VertexState.Selected)
                _startVertex.State = VertexState.Default;

            if (_startVertex == vertex)
            {
                _startVertex = null;
                ResultStatus = "Стартовая вершина не выбрана";
            }
            else
            {
                _startVertex = vertex;

                // Target не перекрашивается
                if (_startVertex.State != VertexState.Target)
                    _startVertex.State = VertexState.Selected;

                ResultStatus = $"Выбрана стартовая вершина: {vertex.Id}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartBfs))]
        private async Task StartBfs()
        {
            SyncGraphFromDrawTab();

            _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Selected);
            int? startId = _startVertex?.Id;

            var targetVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            int? targetId = targetVertex?.Id;

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск...";

            GraphCanvas.ResetVisuals();

            _startVertex?.State = VertexState.Selected;
            targetVertex?.State = VertexState.Target;

            var resultData = new BfsResult();
            var steps = _graphModel.RunBfs(startId, targetId, resultData);

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(Parameters.AnimationDelayMs);
            }

            // 5. Вывод результатов
            PathLength = resultData.PathLength;
            ParenthesisStructure = resultData.ParenthesisStructure;

            if (resultData.IsTargetFound)
            {
                PathString = string.Join(" -> ", resultData.Path);
                ResultStatus = "Цель найдена!";
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
        /// Копирование графа из вкладки рисования на вкладку BFS.
        /// </summary>
        [RelayCommand]
        public void SyncGraphFromDrawTab()
        {
            if (IsAnimating) return;

            // Сохранение старта для сброса
            int? prevStartId = _startVertex?.Id;

            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

            if (prevStartId.HasValue)
            {
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.Id == prevStartId.Value);

                if (_startVertex != null && _startVertex.State != VertexState.Target)
                    _startVertex.State = VertexState.Selected;
            }
        }
    }
}