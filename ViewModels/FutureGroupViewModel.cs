using System.Collections.ObjectModel;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 期貨分群 ViewModel，管理群組內的期貨項目與市值計算
    /// </summary>
    public class FutureGroupViewModel : BaseViewModel
    {
        public FutureGroupViewModel(string groupName)
        {
            GroupName = groupName;
            Items = new ObservableCollection<FutureItemViewModel>();
        }

        private string _groupName = string.Empty;
        /// <summary>
        /// 群組名稱（如「記憶體」、「AI」）
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set { _groupName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 群組內的期貨項目
        /// </summary>
        public ObservableCollection<FutureItemViewModel> Items { get; }

        /// <summary>
        /// 群組總市值
        /// </summary>
        public decimal TotalExposure => Items.Sum(x => x.Exposure);

        /// <summary>
        /// 群組總市值（萬元）
        /// </summary>
        public string TotalExposureInWan => FormatWan(TotalExposure);

        /// <summary>
        /// 群組總口數
        /// </summary>
        public int TotalLots => Items.Sum(x => x.Lots);

        /// <summary>
        /// 通知重新計算所有彙總屬性
        /// </summary>
        public void NotifyRecalculate()
        {
            OnPropertyChanged(nameof(TotalExposure));
            OnPropertyChanged(nameof(TotalExposureInWan));
            OnPropertyChanged(nameof(TotalLots));
        }
    }
}
