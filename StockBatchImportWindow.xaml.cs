using System.Windows;
using System.Windows.Controls;
using LeverageCalculator.Models;

namespace LeverageCalculator
{
    /// <summary>
    /// 批次匯入股票視窗，雙區塊設計（現股/融資分開輸入）
    /// </summary>
    public partial class StockBatchImportWindow : Window
    {
        /// <summary>
        /// 解析後的匯入項目列表
        /// </summary>
        public List<StockBatchImportItem> ImportedItems { get; private set; } = new List<StockBatchImportItem>();

        public StockBatchImportWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => CashStockTextBox.Focus();
        }

        private void CashStockTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CashPlaceholder.Visibility = string.IsNullOrEmpty(CashStockTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void MarginStockTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MarginPlaceholder.Visibility = string.IsNullOrEmpty(MarginStockTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string cashText = CashStockTextBox.Text?.Trim() ?? string.Empty;
            string marginText = MarginStockTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(cashText) && string.IsNullOrEmpty(marginText))
            {
                ShowError("請至少在一個區塊中輸入資料");
                return;
            }

            List<StockBatchImportItem> items = new List<StockBatchImportItem>();
            List<string> errors = new List<string>();

            ParseBlock(cashText, StockType.Cash, items, errors, "現股");
            ParseBlock(marginText, StockType.Margin, items, errors, "融資");

            if (errors.Count > 0 && items.Count == 0)
            {
                ShowError(string.Join("\n", errors));
                return;
            }

            if (items.Count == 0)
            {
                ShowError("未解析到任何有效的股票項目");
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

        private static void ParseBlock(string text, StockType stockType,
            List<StockBatchImportItem> items, List<string> errors, string blockLabel)
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
                if (parts.Length < 3)
                {
                    errors.Add($"[{blockLabel}] 第 {lineNum} 行格式錯誤: \"{line}\"（需要：股票代號 股數 進場均價）");
                    continue;
                }

                string stockCode = parts[0];
                if (!int.TryParse(parts[1], out int shares) || shares <= 0)
                {
                    errors.Add($"[{blockLabel}] 第 {lineNum} 行股數無效: \"{parts[1]}\"");
                    continue;
                }

                if (!decimal.TryParse(parts[2], out decimal entryPrice) || entryPrice < 0)
                {
                    errors.Add($"[{blockLabel}] 第 {lineNum} 行進場均價無效: \"{parts[2]}\"");
                    continue;
                }

                items.Add(new StockBatchImportItem
                {
                    StockCode = stockCode,
                    Shares = shares,
                    EntryPrice = entryPrice,
                    StockType = stockType
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
    /// 股票批次匯入的單筆解析結果
    /// </summary>
    public class StockBatchImportItem
    {
        public string StockCode { get; init; } = string.Empty;
        public int Shares { get; init; }
        public decimal EntryPrice { get; init; }
        public StockType StockType { get; init; }
    }
}
