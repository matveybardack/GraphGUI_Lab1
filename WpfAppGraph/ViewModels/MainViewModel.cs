using System.Collections.ObjectModel;
namespace WpfAppGraph.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

        public MainViewModel()
        {
            Tabs.Add(new TabItemViewModel
            {
                Header = "Рисование графа",
                ContentViewModel = new DrawGraphVM()
            });

            //TODO: Add more tabs if needed
        }

    }

    public class TabItemViewModel
    {
        public string Header { get; set; }  
        public object ContentViewModel { get; set; }
    }
}
