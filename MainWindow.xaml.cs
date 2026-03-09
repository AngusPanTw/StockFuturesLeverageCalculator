using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LeverageCalculator.ViewModels;

namespace LeverageCalculator
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Register global handler for TextBox focus
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
        }

        /// <summary>
        /// 當 TextBox 獲得焦點時，自動選取所有文字
        /// </summary>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        /// <summary>
        /// 視窗關閉時儲存資料
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            _viewModel.SaveData();
            base.OnClosing(e);
        }

        /// <summary>
        /// 期貨批次匯入按鈕
        /// </summary>
        private async void FutureBatchImportButton_Click(object sender, RoutedEventArgs e)
        {
            BatchImportWindow importWindow = new BatchImportWindow(
                _viewModel.AvailableYears, _viewModel.AvailableMonths,
                _viewModel.NewContractYear, _viewModel.NewContractMonthNum);
            importWindow.Owner = this;

            if (importWindow.ShowDialog() == true && importWindow.ImportedItems.Count > 0)
            {
                try
                {
                    _ = await _viewModel.FutureBatchImportAsync(importWindow.ImportedItems);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批次匯入時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 股票批次匯入按鈕
        /// </summary>
        private async void StockBatchImportButton_Click(object sender, RoutedEventArgs e)
        {
            StockBatchImportWindow importWindow = new StockBatchImportWindow();
            importWindow.Owner = this;

            if (importWindow.ShowDialog() == true && importWindow.ImportedItems.Count > 0)
            {
                try
                {
                    _ = await _viewModel.StockBatchImportAsync(importWindow.ImportedItems);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"股票批次匯入時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
