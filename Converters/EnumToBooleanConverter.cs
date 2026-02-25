using System.Globalization;
using System.Windows.Data;

namespace LeverageCalculator.Converters
{
    /// <summary>
    /// Enum 轉 Boolean 轉換器，用於 RadioButton 與 Enum 的綁定
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 將 Enum 值轉換為 Boolean (用於 RadioButton 的 IsChecked)
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string? enumValue = value.ToString();
            string? targetValue = parameter.ToString();
            return enumValue?.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        /// <summary>
        /// 將 Boolean 值轉換回 Enum (當 RadioButton 被選中時)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            bool useValue = (bool)value;
            string? paramString = parameter.ToString();
            if (useValue && paramString != null)
                return Enum.Parse(targetType, paramString);

            return Binding.DoNothing;
        }
    }
}
