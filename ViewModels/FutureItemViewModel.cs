using LeverageCalculator.Models;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 期貨項目 ViewModel，封裝 FutureItem 並提供屬性變更通知與市值計算
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
        /// 合約名稱
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
                OnPropertyChanged(nameof(PositionDisplay));
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
                OnPropertyChanged(nameof(ContractTypeDisplay));
                NotifyCalculatedProperties();
            }
        }

        /// <summary>
        /// 所屬群組名稱
        /// </summary>
        public string GroupName
        {
            get => _future.GroupName;
            set { _future.GroupName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 每口合約股數 (小型合約100, 大型合約2000)
        /// </summary>
        public int SharesPerLot => IsSmallContract ? 100 : 2000;

        /// <summary>
        /// 合約類型顯示文字
        /// </summary>
        public string ContractTypeDisplay => IsSmallContract ? "小型" : "大型";

        /// <summary>
        /// 多空方向顯示文字
        /// </summary>
        public string PositionDisplay => Position == PositionType.Long ? "多" : "空";

        /// <summary>
        /// 市值 (市價 × 股數 × 口數)
        /// </summary>
        public decimal Exposure => CurrentPrice * SharesPerLot * Lots;

        /// <summary>
        /// 市值（萬元），小數後一位
        /// </summary>
        public string ExposureInWan => FormatWan(Exposure);

        /// <summary>
        /// 取得底層 Model（供序列化使用）
        /// </summary>
        public FutureItem Model => _future;

        private void NotifyCalculatedProperties()
        {
            OnPropertyChanged(nameof(Exposure));
            OnPropertyChanged(nameof(ExposureInWan));
        }
    }
}
