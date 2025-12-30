using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using LeverageCalculator.Models;
using LeverageCalculator.Services;
using LeverageCalculator.Converters;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 主視窗 ViewModel，負責管理所有資產資料、計算邏輯與使用者互動
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly PortfolioStorageService _storageService;

        // --- Collections ---
        /// <summary>
        /// 股票庫存列表
        /// </summary>
        public ObservableCollection<StockItemViewModel> Stocks { get; set; }
        
        /// <summary>
        /// 大台期貨庫存列表
        /// </summary>
        public ObservableCollection<FutureItemViewModel> LargeFutures { get; set; }
        
        /// <summary>
        /// 小台期貨庫存列表
        /// </summary>
        public ObservableCollection<FutureItemViewModel> SmallFutures { get; set; }
        
        // Helper to aggregate futures for calculation
        private IEnumerable<FutureItemViewModel> AllFutures => LargeFutures.Concat(SmallFutures);

        // --- General Assets ---
        private decimal _bankCash;
        /// <summary>
        /// 銀行現金
        /// </summary>
        public decimal BankCash { get => _bankCash; set { _bankCash = value; OnPropertyChanged(); RecalculateAll(); } }

        private decimal _stockSettlementAmount;
        /// <summary>
        /// 股票交割款 (T+2)
        /// </summary>
        public decimal StockSettlementAmount { get => _stockSettlementAmount; set { _stockSettlementAmount = value; OnPropertyChanged(); RecalculateAll(); } }
        
        private decimal _futuresEquity;
        /// <summary>
        /// 期貨權益數
        /// </summary>
        public decimal FuturesEquity { get => _futuresEquity; set { _futuresEquity = value; OnPropertyChanged(); RecalculateAll(); } }

        // --- Add New Stock ---
        private string _newStockName = "";
        /// <summary>
        /// 新增股票名稱
        /// </summary>
        public string NewStockName { get => _newStockName; set { _newStockName = value; OnPropertyChanged(); } }
        private int _newStockShares;
        /// <summary>
        /// 新增股票股數
        /// </summary>
        public int NewStockShares { get => _newStockShares; set { _newStockShares = value; OnPropertyChanged(); } }
        private decimal _newStockTotalCost;
        /// <summary>
        /// 新增股票總成本
        /// </summary>
        public decimal NewStockTotalCost { get => _newStockTotalCost; set { _newStockTotalCost = value; OnPropertyChanged(); } }
        private decimal _newStockMarketValue;
        /// <summary>
        /// 新增股票總市值
        /// </summary>
        public decimal NewStockMarketValue { get => _newStockMarketValue; set { _newStockMarketValue = value; OnPropertyChanged(); } }

        // --- Add New Future ---
        private string _newFutureName = "";
        /// <summary>
        /// 新增期貨名稱
        /// </summary>
        public string NewFutureName { get => _newFutureName; set { _newFutureName = value; OnPropertyChanged(); } }
        private int _newFutureLots;
        /// <summary>
        /// 新增期貨口數
        /// </summary>
        public int NewFutureLots { get => _newFutureLots; set { _newFutureLots = value; OnPropertyChanged(); } }
        private PositionType _newFuturePosition = PositionType.Long;
        /// <summary>
        /// 新增期貨多空方向
        /// </summary>
        public PositionType NewFuturePosition { get => _newFuturePosition; set { _newFuturePosition = value; OnPropertyChanged(); } }
        private decimal _newFutureCostPrice;
        /// <summary>
        /// 新增期貨成本價
        /// </summary>
        public decimal NewFutureCostPrice { get => _newFutureCostPrice; set { _newFutureCostPrice = value; OnPropertyChanged(); } }
        private decimal _newFutureCurrentPrice;
        /// <summary>
        /// 新增期貨目前市價
        /// </summary>
        public decimal NewFutureCurrentPrice { get => _newFutureCurrentPrice; set { _newFutureCurrentPrice = value; OnPropertyChanged(); } }
        private bool _newFutureIsSmallContract;
        /// <summary>
        /// 新增期貨是否為小型合約
        /// </summary>
        public bool NewFutureIsSmallContract { get => _newFutureIsSmallContract; set { _newFutureIsSmallContract = value; OnPropertyChanged(); } }

        // --- Calculated Results ---
        private decimal _totalStockValue;
        /// <summary>
        /// 股票總市值
        /// </summary>
        public decimal TotalStockValue { get => _totalStockValue; private set { _totalStockValue = value; OnPropertyChanged(); } }
        
        private decimal _totalStockProfitLoss;
        /// <summary>
        /// 股票總損益
        /// </summary>
        public decimal TotalStockProfitLoss { get => _totalStockProfitLoss; private set { _totalStockProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalStockProfitLossColor)); } }

        private double _totalStockProfitLossPercentage;
        /// <summary>
        /// 股票總報酬率
        /// </summary>
        public double TotalStockProfitLossPercentage { get => _totalStockProfitLossPercentage; private set { _totalStockProfitLossPercentage = value; OnPropertyChanged(); } }

        /// <summary>
        /// 股票總損益顏色
        /// </summary>
        public string TotalStockProfitLossColor => TotalStockProfitLoss >= 0 ? "Red" : "Green";

        private decimal _totalFuturesProfitLoss;
        /// <summary>
        /// 期貨總損益
        /// </summary>
        public decimal TotalFuturesProfitLoss { get => _totalFuturesProfitLoss; private set { _totalFuturesProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalFuturesProfitLossColor)); } }

        private double _totalFuturesProfitLossPercentage;
        /// <summary>
        /// 期貨總報酬率
        /// </summary>
        public double TotalFuturesProfitLossPercentage { get => _totalFuturesProfitLossPercentage; private set { _totalFuturesProfitLossPercentage = value; OnPropertyChanged(); } }

        /// <summary>
        /// 期貨總損益顏色
        /// </summary>
        public string TotalFuturesProfitLossColor => TotalFuturesProfitLoss >= 0 ? "Red" : "Green";

        private decimal _totalFuturesExposure;
        /// <summary>
        /// 期貨總曝險
        /// </summary>
        public decimal TotalFuturesExposure { get => _totalFuturesExposure; private set { _totalFuturesExposure = value; OnPropertyChanged(); } }
        private decimal _totalExposure;
        /// <summary>
        /// 總曝險 (股票+期貨)
        /// </summary>
        public decimal TotalExposure { get => _totalExposure; private set { _totalExposure = value; OnPropertyChanged(); } }
        private decimal _totalCapital;
        /// <summary>
        /// 總資產 (股票+現金+交割款+權益數)
        /// </summary>
        public decimal TotalCapital { get => _totalCapital; private set { _totalCapital = value; OnPropertyChanged(); } }
        private double _leverage;
        /// <summary>
        /// 槓桿倍數
        /// </summary>
        public double Leverage { get => _leverage; private set { _leverage = value; OnPropertyChanged(); } }
        private string _riskStatus = "";
        /// <summary>
        /// 風險狀態描述
        /// </summary>
        public string RiskStatus { get => _riskStatus; private set { _riskStatus = value; OnPropertyChanged(); } }
        
        // --- Commands ---
        /// <summary>
        /// 新增股票命令
        /// </summary>
        public ICommand AddStockCommand { get; }
        /// <summary>
        /// 刪除股票命令
        /// </summary>
        public ICommand DeleteStockCommand { get; }
        /// <summary>
        /// 新增期貨命令
        /// </summary>
        public ICommand AddFutureCommand { get; }
        /// <summary>
        /// 刪除期貨命令
        /// </summary>
        public ICommand DeleteFutureCommand { get; }

        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");

            Stocks = new ObservableCollection<StockItemViewModel>();
            Stocks.CollectionChanged += OnCollectionChanged;
            
            LargeFutures = new ObservableCollection<FutureItemViewModel>();
            LargeFutures.CollectionChanged += OnCollectionChanged;
            
            SmallFutures = new ObservableCollection<FutureItemViewModel>();
            SmallFutures.CollectionChanged += OnCollectionChanged;

            AddStockCommand = new RelayCommand(ExecuteAddStock);
            DeleteStockCommand = new RelayCommand(ExecuteDeleteStock);
            AddFutureCommand = new RelayCommand(ExecuteAddFuture);
            DeleteFutureCommand = new RelayCommand(ExecuteDeleteFuture);
            
            LoadData();
        }
        
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            }
            RecalculateAll();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RecalculateAll();
        }

        private void RecalculateAll()
        {
            TotalStockValue = Stocks.Sum(s => s.MarketValue);
            var totalStockCost = Stocks.Sum(s => s.TotalCost);
            TotalStockProfitLoss = TotalStockValue - totalStockCost;
            TotalStockProfitLossPercentage = totalStockCost != 0 ? (double)(TotalStockProfitLoss / totalStockCost) : 0;

            TotalFuturesExposure = AllFutures.Sum(f => f.Exposure);
            TotalFuturesProfitLoss = AllFutures.Sum(f => f.ProfitLoss);
            var totalFuturesCostValue = AllFutures.Sum(f => f.CostPrice * f.SharesPerLot * f.Lots);
            TotalFuturesProfitLossPercentage = totalFuturesCostValue != 0 ? (double)(TotalFuturesProfitLoss / totalFuturesCostValue) : 0;

            TotalExposure = TotalStockValue + TotalFuturesExposure;
            TotalCapital = TotalStockValue + BankCash + StockSettlementAmount + FuturesEquity;

            if (TotalCapital > 0)
            {
                Leverage = (double)(TotalExposure / TotalCapital);

                if (Leverage <= 1.0) RiskStatus = "穩定";
                else if (Leverage <= 2.0) RiskStatus = "適中";
                else RiskStatus = "高風險";
            }
            else
            {
                Leverage = 0;
                RiskStatus = "危險 (資產 <= 0)";
            }
        }

        private void ExecuteAddStock(object? obj)
        {
            var newStock = new StockItem
            {
                Name = NewStockName,
                Shares = NewStockShares,
                TotalCost = NewStockTotalCost,
                MarketValue = NewStockMarketValue
            };
            Stocks.Add(new StockItemViewModel(newStock));
            
            // Clear inputs
            NewStockName = "";
            NewStockShares = 0;
            NewStockTotalCost = 0;
            NewStockMarketValue = 0;
        }

        private void ExecuteDeleteStock(object? obj)
        {
            if (obj is StockItemViewModel stock)
            {
                Stocks.Remove(stock);
            }
        }
        
        private void ExecuteAddFuture(object? obj)
        {
            var newFuture = new FutureItem
            {
                Name = NewFutureName,
                Lots = NewFutureLots,
                Position = NewFuturePosition,
                CostPrice = NewFutureCostPrice,
                CurrentPrice = NewFutureCurrentPrice,
                IsSmallContract = NewFutureIsSmallContract
            };
            
            var vm = new FutureItemViewModel(newFuture);
            if (newFuture.IsSmallContract)
            {
                SmallFutures.Add(vm);
            }
            else
            {
                LargeFutures.Add(vm);
            }

            // Clear inputs
            NewFutureName = "";
            NewFutureLots = 0;
            NewFutureCostPrice = 0;
            NewFutureCurrentPrice = 0;
            NewFutureIsSmallContract = false;
        }

        private void ExecuteDeleteFuture(object? obj)
        {
            if (obj is FutureItemViewModel future)
            {
                if (LargeFutures.Contains(future))
                {
                    LargeFutures.Remove(future);
                }
                else if (SmallFutures.Contains(future))
                {
                    SmallFutures.Remove(future);
                }
            }
        }

        private void LoadData()
        {
            var portfolio = _storageService.LoadPortfolio();
            if (portfolio == null) 
            {
                RecalculateAll();
                return;
            }

            BankCash = portfolio.BankCash;
            StockSettlementAmount = portfolio.StockSettlementAmount;
            FuturesEquity = portfolio.FuturesEquity;

            Stocks.Clear();
            foreach (var stock in portfolio.Stocks)
            {
                Stocks.Add(new StockItemViewModel(stock));
            }

            LargeFutures.Clear();
            SmallFutures.Clear();
            foreach (var future in portfolio.Futures)
            {
                var vm = new FutureItemViewModel(future);
                if (future.IsSmallContract)
                {
                    SmallFutures.Add(vm);
                }
                else
                {
                    LargeFutures.Add(vm);
                }
            }
            
            RecalculateAll();
        }

        public void SaveData()
        {
            var portfolio = new Portfolio
            {
                BankCash = this.BankCash,
                StockSettlementAmount = this.StockSettlementAmount,
                FuturesEquity = this.FuturesEquity,
                Stocks = this.Stocks.Select(vm => vm.Model).ToList(),
                Futures = this.AllFutures.Select(vm => vm.Model).ToList()
            };
            _storageService.SavePortfolio(portfolio);
        }
    }
}
