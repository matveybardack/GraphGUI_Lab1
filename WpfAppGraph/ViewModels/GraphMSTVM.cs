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
    public partial class GraphMSTVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        [ObservableProperty]
        private MstAlgorithmType _algorithmType = MstAlgorithmType.Kruskal;

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private string _componentsOutput;

        [ObservableProperty]
        private bool _isResultAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartMstCommand))]
        [NotifyCanExecuteChangedFor(nameof(SyncGraphCommand))]
        private bool _isAnimating;

        public GraphMSTVM(GraphModel model, DrawGraphVM sourceDrawVM)
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
        private async Task StartMst()
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

            var result = new MstResult();
            IEnumerable<AlgorithmStep> steps;

            // Выбор алгоритма
            steps = (AlgorithmType) switch
            {
                MstAlgorithmType.Kruskal => _graphModel.RunKruskalAlgorithm(result),
                MstAlgorithmType.Prim => _graphModel.RunPrimAlgorithm(result)
            };

            // Предварительная проверка
            if (!string.IsNullOrEmpty(result.StatusMessage))
                ResultStatus = result.StatusMessage;

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(Parameters.AnimationDelayMs); // Скорость анимации
            }

            ResultStatus = result.StatusMessage;
            ComponentsOutput = result.MstLength.ToString();

            IsAnimating = false;
        }

        private bool CanInteract() => !IsAnimating;
    }
}
