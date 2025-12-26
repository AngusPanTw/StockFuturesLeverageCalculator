using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 基礎 ViewModel，實作 INotifyPropertyChanged 介面
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
