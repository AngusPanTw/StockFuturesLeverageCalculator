using System.Windows;
using System.Windows.Controls;

namespace LeverageCalculator
{
    /// <summary>
    /// 批次匯入視窗，雙區塊設計（大型/小型合約分開輸入）
    /// </summary>
    public partial class BatchImportWindow : Window
    {
        /// <summary>
        /// 解析後的匯入項目列表
        /// </summary>
        public List<BatchImportItem> ImportedItems { get; private set; } = new List<BatchImportItem>();

        public BatchImportWindow(List<string> availableYears, List<string> availableMonths,
            string defaultYear, string defaultMonth)
        {
            InitializeComponent();

            YearComboBox.ItemsSource = availableYears;
            YearComboBox.SelectedItem = defaultYear;

            MonthComboBox.ItemsSource = availableMonths;
            MonthComboBox.SelectedItem = defaultMonth;

            Loaded += (_, _) => LargeContractTextBox.Focus();
        }

        private void LargeContractTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LargePlaceholder.Visibility = string.IsNullOrEmpty(LargeContractTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SmallContractTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SmallPlaceholder.Visibility = string.IsNullOrEmpty(SmallContractTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string? selectedYear = YearComboBox.SelectedItem as string;
            string? selectedMonth = MonthComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedYear) || string.IsNullOrEmpty(selectedMonth))
            {
                ShowError("請選擇合約年份與月份");
                return;
            }

            string largeText = LargeContractTextBox.Text?.Trim() ?? string.Empty;
            string smallText = SmallContractTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(largeText) && string.IsNullOrEmpty(smallText))
            {
                ShowError("請至少在一個合約區塊中輸入資料");
                return;
            }

            List<BatchImportItem> items = new List<BatchImportItem>();
            List<string> errors = new List<string>();

            ParseBlock(largeText, isSmallContract: false, selectedYear, selectedMonth, items, errors, "大型合約");
            ParseBlock(smallText, isSmallContract: true, selectedYear, selectedMonth, items, errors, "小型合約");

            if (errors.Count > 0 && items.Count == 0)
            {
                ShowError(string.Join("\n", errors));
                return;
            }

            if (items.Count == 0)
            {
                ShowError("未解析到任何有效的期貨項目");
                return;
            }

            if (errors.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"解析完成：{items.Count} 筆成功，{errors.Count} 筆有誤\n\n{string.Join("\n", errors)}\n\n是否繼續匯入成功的 {items.Count} 筆？",
                    "部分解析失敗",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ImportedItems = items;
            DialogResult = true;
        }

        private static void ParseBlock(string text, bool isSmallContract, string year, string month,
            List<BatchImportItem> items, List<string> errors, string blockLabel)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int lineNum = i + 1;

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    errors.Add($"[{blockLabel}] 第 {lineNum} 行格式錯誤: \"{line}\"（需要：股票代號 口數）");
                    continue;
                }

                string stockCode = parts[0];
                if (!int.TryParse(parts[1], out int lots) || lots <= 0)
                {
                    errors.Add($"[{blockLabel}] 第 {lineNum} 行口數無效: \"{parts[1]}\"");
                    continue;
                }

                items.Add(new BatchImportItem
                {
                    StockCode = stockCode,
                    Lots = lots,
                    IsSmallContract = isSmallContract,
                    Year = year,
                    Month = month
                });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 批次匯入的單筆解析結果
    /// </summary>
    public class BatchImportItem
    {
        public string StockCode { get; init; } = string.Empty;
        public int Lots { get; init; }
        public bool IsSmallContract { get; init; }
        public string Year { get; init; } = string.Empty;
        public string Month { get; init; } = string.Empty;

        /// <summary>
        /// 組合為合約月份（如 "2603"）
        /// </summary>
        public string ContractMonth => Year + Month;
    }
}
