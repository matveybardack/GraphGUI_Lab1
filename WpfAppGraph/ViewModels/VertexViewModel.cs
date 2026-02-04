using CommunityToolkit.Mvvm.ComponentModel;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.ViewModels
{
    public partial class VertexViewModel : ObservableObject
    {
        public int Id { get; }

        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private string _label;

        [ObservableProperty]
        private VertexState _state;

        [ObservableProperty]
        private string _iterationInfo;

        // Вычисляемые свойства для удобства (центр круга 40x40)
        public double CenterX => X + 20;
        public double CenterY => Y + 20;

        public VertexViewModel(int id, double x, double y)
        {
            Id = id;
            X = x;
            Y = y;
            Label = id.ToString();
            State = VertexState.Default;
            IterationInfo = string.Empty;
        }
    }
}