using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.ViewModels
{
    public partial class GraphSCCVM : ObservableObject
    {
        // Холст для отображения
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();

        private readonly GraphModel _graphModel;
        private readonly DrawGraphVM _sourceDrawVM;

        // --- Результаты ---

        [ObservableProperty]
        private string _resultStatus = "Ожидание запуска...";

        [ObservableProperty]
        private int _componentCount;

        [ObservableProperty]
        private string _componentsOutput; // Текстовое представление списков

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
        /// Синхронизация графа с вкладкой рисования.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        public void SyncGraph()
        {
            GraphCanvas.CloneFrom(_sourceDrawVM.GraphCanvas);

            // Сброс результатов
            IsResultAvailable = false;
            ResultStatus = "Граф загружен. Нажмите 'Найти компоненты'.";
            ComponentsOutput = string.Empty;
            ComponentCount = 0;

            // Сбрасываем цвета на всякий случай
            GraphCanvas.ResetVisuals();
        }

        /// <summary>
        /// Запуск алгоритма поиска SCC.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task StartScc()
        {
            // 1. Авто-синхронизация (для удобства)
            if (GraphCanvas.Vertices.Count == 0)
            {
                SyncGraph();
            }

            if (GraphCanvas.Vertices.Count == 0)
            {
                ResultStatus = "Граф пуст. Нарисуйте граф на первой вкладке.";
                return;
            }

            IsAnimating = true;
            IsResultAvailable = false;
            ResultStatus = "Выполняется поиск компонентов...";

            // Сбрасываем визуальные эффекты перед запуском
            GraphCanvas.ResetVisuals();

            // 2. Запуск алгоритма в модели
            var resultData = new SccResult();
            // Получаем ленивую коллекцию шагов
            var steps = _graphModel.RunFindStronglyConnectedComponents(resultData);

            // 3. Анимация
            foreach (var step in steps)
            {
                GraphCanvas.ApplyAlgorithmStep(step);

                // Небольшая задержка для визуализации процесса
                // Можно уменьшить, если шагов слишком много
                await Task.Delay(150);
            }

            // 4. Формирование отчета
            ComponentCount = resultData.ComponentCount;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resultData.Components.Count; i++)
            {
                var comp = resultData.Components[i];
                // Сортируем вершины внутри компонента для красоты
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