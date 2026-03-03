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

        /// <summary>
        /// 將金額格式化為萬元顯示。整數萬不帶小數，否則顯示一位小數。
        /// 例：1234.0萬 → "1,234萬"、1234.5萬 → "1,234.5萬"
        /// </summary>
        protected static string FormatWan(decimal amount)
        {
            decimal wan = amount / 10000m;
            decimal fraction = wan % 1;
            if (fraction == 0)
            {
                return $"{wan:N0}萬";
            }
            return $"{wan:N1}萬";
        }
    }
}
