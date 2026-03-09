using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

            // 訂閱集合變更：新增/刪除/重設時自動調整欄寬
            SubscribeCollectionAutoFit(_viewModel.CashStocks, AutoFitStockColumns);
            SubscribeCollectionAutoFit(_viewModel.MarginStocks, AutoFitStockColumns);
            SubscribeCollectionAutoFit(_viewModel.LargeFutures, AutoFitFutureColumns);
            SubscribeCollectionAutoFit(_viewModel.SmallFutures, AutoFitFutureColumns);

            // 訂閱收盤價更新完成：IsUpdatingPrices 從 true → false 時調整兩個面板欄寬
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // 視窗載入後執行一次初始調整（載入既有資料時）
            Loaded += (_, _) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    AutoFitStockColumns();
                    AutoFitFutureColumns();
                }, DispatcherPriority.Background);
            };
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

        /// <summary>
        /// 收盤價更新完成後自動調整欄寬
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsUpdatingPrices) && !_viewModel.IsUpdatingPrices)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    AutoFitStockColumns();
                    AutoFitFutureColumns();
                }, DispatcherPriority.Background);
            }
        }

        private void SubscribeCollectionAutoFit(INotifyCollectionChanged collection, Action autoFitAction)
        {
            collection.CollectionChanged += (_, _) =>
                Dispatcher.InvokeAsync(autoFitAction, DispatcherPriority.Background);
        }

        private void AutoFitStockColumns()
        {
            AutoFitColumns(CashStocksGrid);
            AutoFitColumns(MarginStocksGrid);
        }

        private void AutoFitFutureColumns()
        {
            AutoFitColumns(LargeFuturesGrid);
            AutoFitColumns(SmallFuturesGrid);
        }

        /// <summary>
        /// 依內容自動調整 DataGrid 所有欄位寬度（等同雙擊欄位分隔線）
        /// </summary>
        private static void AutoFitColumns(DataGrid dataGrid)
        {
            foreach (DataGridColumn column in dataGrid.Columns)
                column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

            dataGrid.UpdateLayout();

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                if (column.ActualWidth > 0)
                    column.Width = column.ActualWidth;
            }
        }
    }
}
