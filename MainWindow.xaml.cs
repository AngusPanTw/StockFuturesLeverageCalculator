using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LeverageCalculator.ViewModels;

namespace LeverageCalculator
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private Point _dragStartPoint;
        private bool _isDragging;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _viewModel.SaveData();
            base.OnClosing(e);
        }

        // --- Enter 鍵觸發新增 ---

        private async void AddFutureArea_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            try
            {
                string? error = _viewModel.ValidateNewFutureInputs();
                if (error != null)
                {
                    MessageBox.Show(error, "輸入不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string? addError = await _viewModel.AddFutureAsync();
                if (addError != null)
                {
                    MessageBox.Show(addError, "查詢失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增期貨時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            StockCodeTextBox.Focus();
        }

        // --- Drag & Drop: DataGrid → Group ---

        private void FuturesDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private void FuturesDataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (_isDragging)
            {
                return;
            }

            // 確認拖曳的是 DataGrid 行
            DataGrid dataGrid = (DataGrid)sender;
            DataGridRow? row = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
            if (row == null)
            {
                return;
            }

            FutureItemViewModel? item = row.Item as FutureItemViewModel;
            if (item == null)
            {
                return;
            }

            _isDragging = true;
            DataObject dragData = new DataObject("FutureItem", item);
            DragDrop.DoDragDrop(dataGrid, dragData, DragDropEffects.Move);
            _isDragging = false;
        }

        private void GroupCard_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FutureItem"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void GroupCard_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FutureItem"))
            {
                return;
            }

            FutureItemViewModel? item = e.Data.GetData("FutureItem") as FutureItemViewModel;
            if (item == null)
            {
                return;
            }

            // 從 Border 的 Tag 取得群組名稱
            if (sender is FrameworkElement element && element.Tag is string groupName)
            {
                _viewModel.MoveItemToGroup(item, groupName);
            }

            e.Handled = true;
        }

        private void UngroupedArea_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FutureItem"))
            {
                return;
            }

            FutureItemViewModel? item = e.Data.GetData("FutureItem") as FutureItemViewModel;
            if (item == null)
            {
                return;
            }

            _viewModel.RemoveItemFromGroup(item);
            e.Handled = true;
        }

        // --- 點擊按鈕指派群組 ---

        private async void BatchImportButton_Click(object sender, RoutedEventArgs e)
        {
            BatchImportWindow importWindow = new BatchImportWindow(
                _viewModel.AvailableYears, _viewModel.AvailableMonths,
                _viewModel.NewContractYear, _viewModel.NewContractMonthNum);
            importWindow.Owner = this;

            if (importWindow.ShowDialog() == true && importWindow.ImportedItems.Count > 0)
            {
                try
                {
                    _ = await _viewModel.BatchImportAsync(importWindow.ImportedItems);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批次匯入時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GroupNameArea_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            if (!string.IsNullOrWhiteSpace(_viewModel.NewGroupName))
            {
                _viewModel.CreateGroupCommand.Execute(null);
                GroupNameTextBox.Focus();
            }
        }

        private void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox)
            {
                return;
            }

            if (comboBox.SelectedItem is not FutureGroupViewModel selectedGroup)
            {
                return;
            }

            // ComboBox 的 DataContext 是 FutureItemViewModel
            if (comboBox.DataContext is FutureItemViewModel future)
            {
                _viewModel.MoveItemToGroup(future, selectedGroup.GroupName);
            }

            // 重設選取，讓下拉選單回到未選取狀態
            comboBox.SelectedItem = null;
        }

        /// <summary>
        /// 在視覺樹中向上搜尋指定類型的祖先元素
        /// </summary>
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                {
                    return target;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
