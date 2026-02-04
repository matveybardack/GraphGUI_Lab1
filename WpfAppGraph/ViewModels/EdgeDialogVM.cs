using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfAppGraph.ViewModels
{
    public partial class EdgeDialogVM : ObservableObject
    {
        [ObservableProperty]
        private string _weight = "1";

        [ObservableProperty]
        private bool _isDirected;

        private readonly Action<double, bool> _onConfirm;
        private readonly Action _onCancel;

        public EdgeDialogVM(Action<double, bool> onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (!double.TryParse(Weight, out double resultWeight) || resultWeight < 0)
                resultWeight = 1;

            _onConfirm?.Invoke(resultWeight, IsDirected);
        }

        [RelayCommand]
        private void Cancel()
        {
            _onCancel?.Invoke();
        }
    }
}
