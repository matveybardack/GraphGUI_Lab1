using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using WpfAppGraph.Configs;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphDFSVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        [ObservableProperty]
        private DfsAlgorithmType _algorithmType = DfsAlgorithmType.Iterative;

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

        private VertexViewModel? _startVertex;

        public GraphDFSVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;

            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        /// <summary>
        /// Выбор стартовой вершины
        /// </summary>
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

                // Target
                if (_startVertex.State != VertexState.Target)
                    _startVertex.State = VertexState.Selected;

                ResultStatus = $"Выбрана стартовая вершина: {vertex.Id}";
            }
        }

        /// <summary>
        /// Копирование графа
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            // Сохраняем ID, чтобы попытаться восстановить выбор после перерисовки
            int? prevStartId = _startVertex?.Id;

            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

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
                ResultStatus = "Стартовая вершина не выбрана.";
            }

            IsResultAvailable = false;
            ParenthesisStructure = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartDfs()
        {
            SyncGraph();

            var targetVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            int? targetId = targetVertex?.Id;

            if (_startVertex == null)
                _startVertex = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Selected);
            int? startId = _startVertex?.Id;

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск...";

            GraphCanvas.ResetVisuals();

            _startVertex?.State = VertexState.Selected;
            targetVertex?.State = VertexState.Target;

            var resultData = new DfsResult();
            IEnumerable<AlgorithmStep> steps;

            steps = (AlgorithmType) switch
            {
                DfsAlgorithmType.Iterative => _graphModel.RunDfsIterative(startId, targetId, resultData),
                DfsAlgorithmType.Recursive => _graphModel.RunDfsRecursive(startId, targetId, resultData),
                _ => null
            };

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(Parameters.AnimationDelayMs);
            }

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
                ResultStatus = targetId.HasValue ? "Цель недостижима" : "Обход завершен";
            }

            IsResultAvailable = true;
            IsAnimating = false;
        }

        private bool CanInteract() => !IsAnimating;
    }
}
