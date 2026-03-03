using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace LeverageCalculator
{
    /// <summary>
    /// 批次匯入視窗，解析多行文字為期貨項目列表
    /// </summary>
    public partial class BatchImportWindow : Window
    {
        /// <summary>
        /// 解析後的匯入項目列表
        /// </summary>
        public List<BatchImportItem> ImportedItems { get; private set; } = new List<BatchImportItem>();

        private static readonly Regex YearMonthPattern = new Regex(@"^(\d+)年(\d+)月$");

        public BatchImportWindow()
        {
            InitializeComponent();
            InputTextBox.Focus();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(InputTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            string text = InputTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                ShowError("請輸入匯入內容");
                return;
            }

            List<BatchImportItem> items = new List<BatchImportItem>();
            List<string> errors = new List<string>();

            bool isSmall = false;
            string year = string.Empty;
            string month = string.Empty;

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int lineNum = i + 1;

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // 切換大型/小型
                if (line == "大")
                {
                    isSmall = false;
                    continue;
                }
                if (line == "小")
                {
                    isSmall = true;
                    continue;
                }

                // 年月設定
                Match yearMonthMatch = YearMonthPattern.Match(line);
                if (yearMonthMatch.Success)
                {
                    string rawYear = yearMonthMatch.Groups[1].Value;

                    // 支援民國年（3 碼以上）與西元年後兩碼
                    if (int.TryParse(rawYear, out int yearNum) && yearNum > 99)
                    {
                        // 民國年轉西元年後兩碼
                        year = ((yearNum + 1911) % 100).ToString();
                    }
                    else
                    {
                        year = rawYear;
                    }

                    month = yearMonthMatch.Groups[2].Value.PadLeft(2, '0');
                    continue;
                }

                // 代號 + 口數
                string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    errors.Add($"第 {lineNum} 行格式錯誤: \"{line}\"");
                    continue;
                }

                string stockCode = parts[0];
                if (!int.TryParse(parts[1], out int lots) || lots <= 0)
                {
                    errors.Add($"第 {lineNum} 行口數無效: \"{parts[1]}\"");
                    continue;
                }

                if (string.IsNullOrEmpty(year) || string.IsNullOrEmpty(month))
                {
                    errors.Add($"第 {lineNum} 行尚未設定年月");
                    continue;
                }

                items.Add(new BatchImportItem
                {
                    StockCode = stockCode,
                    Lots = lots,
                    IsSmallContract = isSmall,
                    Year = year,
                    Month = month
                });
            }

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
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
