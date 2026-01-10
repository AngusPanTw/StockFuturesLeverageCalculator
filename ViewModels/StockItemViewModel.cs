using LeverageCalculator.Models;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 股票項目 ViewModel，封裝 StockItem 並提供屬性變更通知
    /// </summary>
    public class StockItemViewModel : BaseViewModel
    {
        private readonly StockItem _stock;

        public StockItemViewModel(StockItem stock)
        {
            _stock = stock;
        }

        /// <summary>
        /// 股票名稱
        /// </summary>
        public string Name
        {
            get => _stock.Name;
            set { _stock.Name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 持有股數
        /// </summary>
        public int Shares
        {
            get => _stock.Shares;
            set 
            { 
                _stock.Shares = value; 
                OnPropertyChanged(); 
            }
        }

        /// <summary>
        /// 總市值
        /// </summary>
        public decimal MarketValue
        {
            get => _stock.MarketValue;
            set 
            { 
                _stock.MarketValue = value; 
                OnPropertyChanged(); 
                // Market value changed, we don't recalculate profit/loss anymore as it is manual input
            }
        }
        
        /// <summary>
        /// 未實現損益 (使用者輸入)
        /// </summary>
        public decimal ProfitLoss 
        {
             get => _stock.ProfitLoss;
             set
             {
                 _stock.ProfitLoss = value;
                 OnPropertyChanged();
                 OnPropertyChanged(nameof(ProfitLossColor));
             }
        }
        
        /// <summary>
        /// 報酬率 (使用者輸入, 儲存原始值 0.1 = 10%)
        /// </summary>
        public double ProfitLossPercentage 
        {
            get => _stock.ProfitLossPercentage;
            set
            {
                _stock.ProfitLossPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfitLossPercentage100)); // Update the display value
            }
        }

        /// <summary>
        /// 報酬率顯示值 (使用者輸入 10 = 10%)
        /// 用於 UI 綁定，自動轉換百分比
        /// </summary>
        public double ProfitLossPercentage100
        {
            // Use Math.Round to prevent floating point artifacts (e.g. 0.87 becoming 0.86999...)
            get => System.Math.Round(ProfitLossPercentage * 100, 6);
            set => ProfitLossPercentage = value / 100;
        }

        /// <summary>
        /// 損益顏色 (>=0 紅色, <0 綠色)
        /// </summary>
        public string ProfitLossColor => ProfitLoss >= 0 ? "Red" : "Green";

        // Expose the model for saving purposes
        public StockItem Model => _stock;
    }
}
