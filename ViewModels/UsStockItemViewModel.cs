using LeverageCalculator.Models;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 美股項目 ViewModel，封裝 UsStockItem 並提供屬性變更通知
    /// </summary>
    public class UsStockItemViewModel : BaseViewModel
    {
        private readonly UsStockItem _stock;

        public UsStockItemViewModel(UsStockItem stock)
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
        /// 總成本
        /// </summary>
        public decimal TotalCost
        {
            get => _stock.TotalCost;
            set 
            { 
                _stock.TotalCost = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ProfitLoss));
                OnPropertyChanged(nameof(ProfitLossPercentage));
                OnPropertyChanged(nameof(ProfitLossColor));
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
                OnPropertyChanged(nameof(ProfitLoss));
                OnPropertyChanged(nameof(ProfitLossPercentage));
                OnPropertyChanged(nameof(ProfitLossColor));
            }
        }
        
        /// <summary>
        /// 未實現損益 (市值 - 成本)
        /// </summary>
        public decimal ProfitLoss => MarketValue - TotalCost;
        
        /// <summary>
        /// 報酬率 (%)
        /// </summary>
        public double ProfitLossPercentage => TotalCost != 0 ? (double)(ProfitLoss / TotalCost) : 0;

        /// <summary>
        /// 損益顏色 (>=0 紅色, <0 綠色)
        /// </summary>
        public string ProfitLossColor => ProfitLoss >= 0 ? "Red" : "Green";

        // Expose the model for saving purposes
        public UsStockItem Model => _stock;
    }
}
