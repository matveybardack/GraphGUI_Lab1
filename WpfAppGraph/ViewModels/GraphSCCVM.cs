using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfAppGraph.Configs;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphSCCVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private int _componentCount;

        [ObservableProperty]
        private string _componentsOutput;

        [ObservableProperty]
        private bool _isResultAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartSccCommand))]
        [NotifyCanExecuteChangedFor(nameof(SyncGraphCommand))]
        private bool _isAnimating;

        public GraphSCCVM(GraphModel model, DrawGraphVM sourceDrawVM)
        {
            _graphModel = model;
            _sourceDrawVM = sourceDrawVM;
        }

        /// <summary>
        /// Синхронизация графа.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

            // Сброс результатов
            IsResultAvailable = false;
            ResultStatus = "Ожидание запуска...";
            ComponentsOutput = string.Empty;
            ComponentCount = 0;

            GraphCanvas.ResetVisuals();
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartScc()
        {
            SyncGraph();

            if (GraphCanvas.Vertices.Count == 0)
            {
                ResultStatus = "Граф пуст. Нарисуйте граф на первой вкладке.";
                return;
            }

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск компонентов...";

            var resultData = new SccResult();
            var steps = _graphModel.RunFindStronglyConnectedComponents(resultData);

            // Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);
                await Task.Delay(Parameters.AnimationDelayMs);
            }

            ComponentCount = resultData.ComponentCount;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resultData.Components.Count; i++)
            {
                var comp = resultData.Components[i];
                comp.Sort();
                sb.AppendLine($"SCC #{i + 1}: {{ {string.Join(", ", comp)} }}");
            }
            ComponentsOutput = sb.ToString();

            ResultStatus = $"Найдено компонентов: {ComponentCount}";
            IsResultAvailable = true;
            IsAnimating = false;
        }

        private bool CanInteract() => !IsAnimating;
    }
}