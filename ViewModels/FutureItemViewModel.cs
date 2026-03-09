using LeverageCalculator.Models;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 期貨項目 ViewModel，封裝 FutureItem 並提供屬性變更通知與計算邏輯
    /// </summary>
    public class FutureItemViewModel : BaseViewModel
    {
        private readonly FutureItem _future;

        public FutureItemViewModel(FutureItem future)
        {
            _future = future;
        }

        /// <summary>
        /// 標的股票代號（如 "2330"）
        /// </summary>
        public string UnderlyingStockCode
        {
            get => _future.UnderlyingStockCode;
            set { _future.UnderlyingStockCode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 合約月份（如 "2603"）
        /// </summary>
        public string ContractMonth
        {
            get => _future.ContractMonth;
            set { _future.ContractMonth = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 完整期貨代碼（自動組合，如 "CDF202603"）
        /// </summary>
        public string StockCode
        {
            get => _future.StockCode;
            set { _future.StockCode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 合約名稱（自動從 API 填入）
        /// </summary>
        public string Name
        {
            get => _future.Name;
            set { _future.Name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 持倉口數
        /// </summary>
        public int Lots
        {
            get => _future.Lots;
            set
            {
                _future.Lots = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 多空方向
        /// </summary>
        public PositionType Position
        {
            get => _future.Position;
            set
            {
                _future.Position = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 成本價格
        /// </summary>
        public decimal CostPrice
        {
            get => _future.CostPrice;
            set
            {
                _future.CostPrice = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 目前市價
        /// </summary>
        public decimal CurrentPrice
        {
            get => _future.CurrentPrice;
            set
            {
                _future.CurrentPrice = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 是否為小型合約
        /// </summary>
        public bool IsSmallContract
        {
            get => _future.IsSmallContract;
            set
            {
                _future.IsSmallContract = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SharesPerLot));
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 每口合約股數 (小型合約100, 大型合約2000)
        /// </summary>
        public int SharesPerLot => IsSmallContract ? 100 : 2000;

        /// <summary>
        /// 曝險市值 (市價 * 股數 * 口數)
        /// </summary>
        public decimal Exposure => CurrentPrice * SharesPerLot * Lots;

        /// <summary>
        /// 曝險市值（萬元，四捨五入至小數點後一位）
        /// </summary>
        public decimal ExposureWan => Math.Round(Exposure / 10000m, 1, MidpointRounding.AwayFromZero);

        /// <summary>
        /// 未實現損益
        /// </summary>
        public decimal ProfitLoss
        {
            get
            {
                decimal diff = Position == PositionType.Long
                    ? CurrentPrice - CostPrice
                    : CostPrice - CurrentPrice;
                return diff * SharesPerLot * Lots;
            }
        }

        /// <summary>
        /// 損益（萬元，四捨五入至小數點後一位）
        /// </summary>
        public decimal ProfitLossWan => Math.Round(ProfitLoss / 10000m, 1, MidpointRounding.AwayFromZero);

        /// <summary>
        /// 報酬率 (%)
        /// </summary>
        public double ProfitLossPercentage
        {
            get
            {
                decimal initialValue = CostPrice * SharesPerLot * Lots;
                return initialValue != 0 ? (double)(ProfitLoss / initialValue) : 0;
            }
        }

        /// <summary>
        /// 損益顏色 (&gt;=0 紅色, &lt;0 綠色)
        /// </summary>
        public string ProfitLossColor => ProfitLoss >= 0 ? "Red" : "Green";

        /// <summary>
        /// 取得底層 Model（供序列化使用）
        /// </summary>
        public FutureItem Model => _future;

        private void NotifyCalculatedProperties()
        {
            OnPropertyChanged(nameof(Exposure));
            OnPropertyChanged(nameof(ExposureWan));
            OnPropertyChanged(nameof(ProfitLoss));
            OnPropertyChanged(nameof(ProfitLossWan));
            OnPropertyChanged(nameof(ProfitLossPercentage));
            OnPropertyChanged(nameof(ProfitLossColor));
        }
    }
}
