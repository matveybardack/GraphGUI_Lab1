using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphEulerVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        [ObservableProperty]
        private EulerAlgorithmType _algorithmType = EulerAlgorithmType.Hierholzer;

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private string _pathString;

        [ObservableProperty]
        private bool _isResultAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartEulerCommand))]
        [NotifyCanExecuteChangedFor(nameof(SyncGraphCommand))]
        private bool _isAnimating;

        public GraphEulerVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);
            IsResultAvailable = false;
            ResultStatus = "Ожидание запуска...";
            PathString = string.Empty;
            GraphCanvas.ResetVisuals();
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartEuler()
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

            var result = new EulerResult();
            IEnumerable<AlgorithmStep> steps;

            // Выбор алгоритма
            steps = (AlgorithmType) switch
            {
                EulerAlgorithmType.Fleury => _graphModel.RunFleuryAlgorithm(result),
                EulerAlgorithmType.Hierholzer => _graphModel.RunHierholzerAlgorithm(result),
                _ => null
            };

            // Предварительная проверка
            if (!string.IsNullOrEmpty(result.StatusMessage))
                ResultStatus = result.StatusMessage;

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(300); // Скорость анимации
            }

            if (result.IsSuccess)
            {
                ResultStatus = $"{result.StatusMessage}. Успешно!";
                PathString = string.Join(" -> ", result.Path);
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
