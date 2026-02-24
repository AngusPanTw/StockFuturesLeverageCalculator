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
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 進場均價
        /// </summary>
        public decimal EntryPrice
        {
            get => _stock.EntryPrice;
            set
            {
                _stock.EntryPrice = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 現價
        /// </summary>
        public decimal CurrentPrice
        {
            get => _stock.CurrentPrice;
            set
            {
                _stock.CurrentPrice = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 持有類型（現股/融資）
        /// </summary>
        public StockType StockType
        {
            get => _stock.StockType;
            set
            {
                _stock.StockType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StockTypeDisplay));
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 融資成數（券商借出比例）
        /// </summary>
        public decimal MarginRatio
        {
            get => _stock.MarginRatio;
            set
            {
                _stock.MarginRatio = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 類型顯示文字
        /// </summary>
        public string StockTypeDisplay => StockType == StockType.Cash ? "現股" : "融資";

        /// <summary>
        /// 總成本 = 進場均價 x 股數
        /// </summary>
        public decimal TotalCost => EntryPrice * Shares;

        /// <summary>
        /// 總市值 = 現價 x 股數
        /// </summary>
        public decimal MarketValue => CurrentPrice * Shares;

        /// <summary>
        /// 融資金額（現股為 0；融資 = 總成本 x 融資成數）
        /// </summary>
        public decimal LoanAmount => StockType == StockType.Margin ? TotalCost * MarginRatio : 0;

        /// <summary>
        /// 自備款（現股 = 總成本；融資 = 總成本 x (1 - 融資成數)）
        /// </summary>
        public decimal SelfFunded => StockType == StockType.Margin ? TotalCost * (1 - MarginRatio) : TotalCost;

        /// <summary>
        /// 未實現損益 = (現價 - 進場均價) x 股數
        /// </summary>
        public decimal ProfitLoss => (CurrentPrice - EntryPrice) * Shares;

        /// <summary>
        /// 報酬率（現股: 損益/總成本；融資: 損益/自備款）
        /// </summary>
        public double ProfitLossPercentage
        {
            get
            {
                decimal basis = StockType == StockType.Margin ? SelfFunded : TotalCost;
                return basis != 0 ? (double)(ProfitLoss / basis) : 0;
            }
        }

        /// <summary>
        /// 損益顏色 (>=0 紅色, &lt;0 綠色)
        /// </summary>
        public string ProfitLossColor => ProfitLoss >= 0 ? "Red" : "Green";

        /// <summary>
        /// 取得底層 Model（供序列化使用）
        /// </summary>
        public StockItem Model => _stock;

        private void NotifyCalculatedProperties()
        {
            OnPropertyChanged(nameof(TotalCost));
            OnPropertyChanged(nameof(MarketValue));
            OnPropertyChanged(nameof(LoanAmount));
            OnPropertyChanged(nameof(SelfFunded));
            OnPropertyChanged(nameof(ProfitLoss));
            OnPropertyChanged(nameof(ProfitLossPercentage));
            OnPropertyChanged(nameof(ProfitLossColor));
        }
    }
}
