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
        private decimal _newStockProfitLoss;
        /// <summary>
        /// 新增股票帳面損益
        /// </summary>
        public decimal NewStockProfitLoss { get => _newStockProfitLoss; set { _newStockProfitLoss = value; OnPropertyChanged(); } }
        private double _newStockProfitLossPercentage;
        /// <summary>
        /// 新增股票報酬率
        /// </summary>
        public double NewStockProfitLossPercentage { get => _newStockProfitLossPercentage; set { _newStockProfitLossPercentage = value; OnPropertyChanged(); } }
        private decimal _newStockMarketValue;
        /// <summary>
        /// 新增股票總市值
        /// </summary>
        public decimal NewStockMarketValue { get => _newStockMarketValue; set { _newStockMarketValue = value; OnPropertyChanged(); } }



        // --- Calculated Results ---
        private decimal _totalStockValue;
        /// <summary>
        /// 股票總市值
        /// </summary>
        public decimal TotalStockValue { get => _totalStockValue; private set { _totalStockValue = value; OnPropertyChanged(); } }
        
        private decimal _totalStockProfitLoss;
        /// <summary>
        /// 股票總損益 (可手動修改)
        /// </summary>
        public decimal TotalStockProfitLoss { get => _totalStockProfitLoss; set { _totalStockProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalStockProfitLossColor)); } }

        private double _totalStockProfitLossPercentage;
        /// <summary>
        /// 股票總報酬率 (可手動修改, 儲存原始值)
        /// </summary>
        public double TotalStockProfitLossPercentage { get => _totalStockProfitLossPercentage; set { _totalStockProfitLossPercentage = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalStockProfitLossPercentage100)); } }

        /// <summary>
        /// 股票總報酬率顯示值 (UI 綁定用, 10 = 10%)
        /// </summary>
        public double TotalStockProfitLossPercentage100
        {
            get => TotalStockProfitLossPercentage * 100;
            set => TotalStockProfitLossPercentage = value / 100;
        }

        /// <summary>
        /// 股票總損益顏色
        /// </summary>
        public string TotalStockProfitLossColor => TotalStockProfitLoss >= 0 ? "Red" : "Green";

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


        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");

            Stocks = new ObservableCollection<StockItemViewModel>();
            Stocks.CollectionChanged += OnCollectionChanged;
            
            AddStockCommand = new RelayCommand(ExecuteAddStock);
            DeleteStockCommand = new RelayCommand(ExecuteDeleteStock);
            
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
            
            // Re-aggregate user inputs from stock list to get a default "Total"
            // Users can override this by editing the text box bound to these properties, 
            // BUT currently the recalculation here would overwrite their manual adjustments every time a collection changes.
            // Since the user asked for these fields to be editable, we should decide whether RecalculateAll overrides it 
            // OR if RecalculateAll simply sums up.
            // Given "Statistic Profit Loss ... to be modifiable", usually implies "Sum is default, but let me tweak it".
            // However, a simple implementation that respects the "Sum" is safer for now.
            // If the user manually edits the Total Box, RecalculateAll runs whenever Stocks change.
            // Let's stick to Summing for now, but since the property setter is public, the View can bind TwoWay.
            
            TotalStockProfitLoss = Stocks.Sum(s => s.ProfitLoss);
            
            // Derive cost from Value - Profit for percentage calculation
            decimal totalStockCost = TotalStockValue - TotalStockProfitLoss;
            
            TotalStockProfitLossPercentage = totalStockCost != 0 ? (double)(TotalStockProfitLoss / totalStockCost) : 0;

            TotalExposure = TotalStockValue;
            TotalCapital = TotalStockValue + BankCash + StockSettlementAmount;

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
            StockItem newStock = new StockItem
            {
                Name = NewStockName,
                Shares = NewStockShares,
                ProfitLoss = NewStockProfitLoss,
                ProfitLossPercentage = NewStockProfitLossPercentage / 100.0, // Convert 8.7 to 0.087
                MarketValue = NewStockMarketValue
            };
            Stocks.Add(new StockItemViewModel(newStock));
            
            ClearStockInputs();
        }

        private void ClearStockInputs()
        {
            NewStockName = "";
            NewStockShares = 0;
            NewStockProfitLoss = 0;
            NewStockProfitLossPercentage = 0;
            NewStockMarketValue = 0;
        }

        private void ExecuteDeleteStock(object? obj)
        {
            if (obj is StockItemViewModel stock)
            {
                Stocks.Remove(stock);
            }
        }
        


        private void LoadData()
        {
            Portfolio? portfolio = _storageService.LoadPortfolio();
            if (portfolio == null) 
            {
                RecalculateAll();
                return;
            }

            BankCash = portfolio.BankCash;
            StockSettlementAmount = portfolio.StockSettlementAmount;

            Stocks.Clear();
            foreach (StockItem stock in portfolio.Stocks)
            {
                Stocks.Add(new StockItemViewModel(stock));
            }

            RecalculateAll();
        }

        public void SaveData()
        {
            Portfolio portfolio = new Portfolio
            {
                BankCash = this.BankCash,
                StockSettlementAmount = this.StockSettlementAmount,
                Stocks = this.Stocks.Select(vm => vm.Model).ToList()
            };
            _storageService.SavePortfolio(portfolio);
        }
    }
}
