using CommunityToolkit.Mvvm.ComponentModel;
using WpfAppGraph.Configs;
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

        [ObservableProperty]
        private double _vertexSquareSize = Parameters.VertexRadius * 2;

        public double CenterX => X + Parameters.VertexRadius;
        public double CenterY => Y + Parameters.VertexRadius;

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