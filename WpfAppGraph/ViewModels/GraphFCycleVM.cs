using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using WpfAppGraph.Configs;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphFCycleVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        [ObservableProperty]
        private GraphTraversalType _algorithmType = GraphTraversalType.BFS;

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private string _componentsOutput;

        [ObservableProperty]
        private bool _isResultAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartFundamentalCommand))]
        [NotifyCanExecuteChangedFor(nameof(SyncGraphCommand))]
        private bool _isAnimating;

        public GraphFCycleVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;
        }

        /// <summary>
        /// Синхронизация графа
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);
            IsResultAvailable = false;
            ResultStatus = "Ожидание запуска...";
            ComponentsOutput = string.Empty;
            GraphCanvas.ResetVisuals();
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartFundamental()
        {
            SyncGraph();

            if (GraphCanvas.Vertices.Count == 0)
            {
                ResultStatus = "Граф пуст.";
                return;
            }

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск...";

            var result = new FundamentalCyclesResult();
            IEnumerable<AlgorithmStep> steps;

            // Выбор алгоритма
            steps = _graphModel.RunFundamentalCyclesAlgorithm(result, AlgorithmType);

            // Предварительная проверка
            if (!string.IsNullOrEmpty(result.StatusMessage))
                ResultStatus = result.StatusMessage;

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(Parameters.AnimationDelayMs); // Скорость анимации
            }

            if (result.IsSuccess)
            {
                ResultStatus = $"{result.StatusMessage}. Успешно!";

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Components.Count; i++)
                {
                    var comp = result.Components[i];
                    comp.Sort();
                    sb.AppendLine($"Cycle #{i + 1}: {{ {string.Join(", ", comp)} }}");
                }
                ComponentsOutput = sb.ToString();
                IsResultAvailable = true;
            }
            else
            {
                if (!IsResultAvailable)
                    ResultStatus = $"Неудача: {result.StatusMessage}";
            }

            IsAnimating = false;
        }

        private bool CanInteract() => !IsAnimating;
    }
}
