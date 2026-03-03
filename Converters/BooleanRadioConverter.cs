using System.Globalization;
using System.Windows.Data;

namespace LeverageCalculator.Converters
{
    /// <summary>
    /// Boolean 值轉換器，用於 RadioButton 與 bool 屬性的雙向綁定。
    /// ConverterParameter 為 "True" 或 "False"，表示此 RadioButton 對應的 bool 值。
    /// </summary>
    public class BooleanRadioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                bool targetBool = bool.Parse(paramString);
                return boolValue == targetBool;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string paramString)
            {
                return bool.Parse(paramString);
            }
            return Binding.DoNothing;
        }
    }
}
