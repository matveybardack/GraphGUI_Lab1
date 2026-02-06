using System.Collections.ObjectModel;
namespace WpfAppGraph.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

        public DrawGraphVM DrawGraph { get; } = new DrawGraphVM();

        public MainViewModel()
        {
            var sharedModel = DrawGraph.GetGraphModel();

            Tabs.Add(new TabItemViewModel
            {
                Header = "Рисование графа",
                ContentViewModel = DrawGraph
            });

            Tabs.Add(new TabItemViewModel
            {
                Header = "Поиск в ширину",
                ContentViewModel = new GraphBFSVM(sharedModel, DrawGraph)
            });

            Tabs.Add(new TabItemViewModel
            {
                Header = "Поиск в глубину",
                ContentViewModel = new GraphDFSVM(sharedModel, DrawGraph)
            });

            Tabs.Add(new TabItemViewModel
            {
                Header = "Сильная связность",
                ContentViewModel = new GraphSCCVM(sharedModel, DrawGraph)
            });

            Tabs.Add(new TabItemViewModel
            {
                Header = "Циклы Эйлера",
                ContentViewModel = new GraphEulerVM(sharedModel, DrawGraph)
            });
        }

    }

    public class TabItemViewModel
    {
        public string Header { get; set; }  
        public object ContentViewModel { get; set; }
    }
}
