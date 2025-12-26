using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    }
}