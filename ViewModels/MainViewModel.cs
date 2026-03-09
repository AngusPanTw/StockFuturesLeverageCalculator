using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using LeverageCalculator.Models;
using LeverageCalculator.Services;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 主視窗 ViewModel，負責管理所有資產資料、計算邏輯與使用者互動
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly PortfolioStorageService _storageService;
        private readonly StockPriceService _stockPriceService;
        private readonly ContractMappingService _contractMappingService;

        // --- Collections ---
        /// <summary>
        /// 現股庫存列表
        /// </summary>
        public ObservableCollection<StockItemViewModel> CashStocks { get; set; }

        /// <summary>
        /// 融資庫存列表
        /// </summary>
        public ObservableCollection<StockItemViewModel> MarginStocks { get; set; }

        private IEnumerable<StockItemViewModel> AllStocks => CashStocks.Concat(MarginStocks);

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
        private string _newStockCode = string.Empty;
        /// <summary>
        /// 新增股票代號
        /// </summary>
        public string NewStockCode { get => _newStockCode; set { _newStockCode = value; OnPropertyChanged(); } }
        private string _newStockName = string.Empty;
        /// <summary>
        /// 新增股票名稱
        /// </summary>
        public string NewStockName { get => _newStockName; set { _newStockName = value; OnPropertyChanged(); } }
        private int _newStockShares;
        /// <summary>
        /// 新增股票股數
        /// </summary>
        public int NewStockShares { get => _newStockShares; set { _newStockShares = value; OnPropertyChanged(); } }
        private decimal _newStockEntryPrice;
        /// <summary>
        /// 新增股票進場均價
        /// </summary>
        public decimal NewStockEntryPrice { get => _newStockEntryPrice; set { _newStockEntryPrice = value; OnPropertyChanged(); } }
        private decimal _newStockCurrentPrice;
        /// <summary>
        /// 新增股票現價
        /// </summary>
        public decimal NewStockCurrentPrice { get => _newStockCurrentPrice; set { _newStockCurrentPrice = value; OnPropertyChanged(); } }
        private StockType _newStockType = StockType.Cash;
        /// <summary>
        /// 新增股票類型（現股/融資）
        /// </summary>
        public StockType NewStockType { get => _newStockType; set { _newStockType = value; OnPropertyChanged(); } }
        private decimal _newStockMarginRatio = 0.6m;
        /// <summary>
        /// 新增股票融資成數
        /// </summary>
        public decimal NewStockMarginRatio { get => _newStockMarginRatio; set { _newStockMarginRatio = value; OnPropertyChanged(); } }

        // --- Calculated Results ---
        private decimal _cashStockValue;
        /// <summary>
        /// 現股總市值
        /// </summary>
        public decimal CashStockValue { get => _cashStockValue; private set { _cashStockValue = value; OnPropertyChanged(); } }

        private decimal _marginStockValue;
        /// <summary>
        /// 融資股票總市值
        /// </summary>
        public decimal MarginStockValue { get => _marginStockValue; private set { _marginStockValue = value; OnPropertyChanged(); } }

        private decimal _totalStockValue;
        /// <summary>
        /// 股票總市值
        /// </summary>
        public decimal TotalStockValue { get => _totalStockValue; private set { _totalStockValue = value; OnPropertyChanged(); } }

        private decimal _cashStockProfitLoss;
        /// <summary>
        /// 現股總損益
        /// </summary>
        public decimal CashStockProfitLoss { get => _cashStockProfitLoss; private set { _cashStockProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(CashStockProfitLossColor)); } }

        private double _cashStockProfitLossPercentage;
        /// <summary>
        /// 現股總報酬率
        /// </summary>
        public double CashStockProfitLossPercentage { get => _cashStockProfitLossPercentage; private set { _cashStockProfitLossPercentage = value; OnPropertyChanged(); } }

        /// <summary>
        /// 現股總損益顏色
        /// </summary>
        public string CashStockProfitLossColor => CashStockProfitLoss >= 0 ? "Red" : "Green";

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

        private decimal _totalMarginSelfFunded;
        /// <summary>
        /// 融資自備金額合計
        /// </summary>
        public decimal TotalMarginSelfFunded { get => _totalMarginSelfFunded; private set { _totalMarginSelfFunded = value; OnPropertyChanged(); } }

        private decimal _totalMarginProfitLoss;
        /// <summary>
        /// 融資未實現損益合計
        /// </summary>
        public decimal TotalMarginProfitLoss { get => _totalMarginProfitLoss; private set { _totalMarginProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalMarginProfitLossColor)); } }

        private double _totalMarginProfitLossPercentage;
        /// <summary>
        /// 融資總報酬率
        /// </summary>
        public double TotalMarginProfitLossPercentage { get => _totalMarginProfitLossPercentage; private set { _totalMarginProfitLossPercentage = value; OnPropertyChanged(); } }

        /// <summary>
        /// 融資損益顏色
        /// </summary>
        public string TotalMarginProfitLossColor => TotalMarginProfitLoss >= 0 ? "Red" : "Green";

        private decimal _totalExposure;
        /// <summary>
        /// 總曝險 (股票)
        /// </summary>
        public decimal TotalExposure { get => _totalExposure; private set { _totalExposure = value; OnPropertyChanged(); } }
        private decimal _totalCapital;
        /// <summary>
        /// 淨資產
        /// </summary>
        public decimal TotalCapital { get => _totalCapital; private set { _totalCapital = value; OnPropertyChanged(); } }
        private double _leverage;
        /// <summary>
        /// 槓桿倍數
        /// </summary>
        public double Leverage { get => _leverage; private set { _leverage = value; OnPropertyChanged(); } }

        // --- Price Update ---
        private string _priceUpdateStatus = string.Empty;
        /// <summary>
        /// 收盤價更新狀態提示
        /// </summary>
        public string PriceUpdateStatus { get => _priceUpdateStatus; set { _priceUpdateStatus = value; OnPropertyChanged(); } }

        private bool _isUpdatingPrices;
        /// <summary>
        /// 是否正在更新收盤價
        /// </summary>
        public bool IsUpdatingPrices { get => _isUpdatingPrices; set { _isUpdatingPrices = value; OnPropertyChanged(); } }

        // --- Commands ---
        public ICommand AddStockCommand { get; }
        public ICommand DeleteStockCommand { get; }
        public ICommand FetchAllPricesCommand { get; }

        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");
            _stockPriceService = new StockPriceService();
            _contractMappingService = new ContractMappingService();

            // 啟動時預熱 TAIFEX 標的資料快取（用於股票名稱查詢）
            _ = _contractMappingService.PreloadCacheAsync();

            CashStocks = new ObservableCollection<StockItemViewModel>();
            CashStocks.CollectionChanged += OnCollectionChanged;

            MarginStocks = new ObservableCollection<StockItemViewModel>();
            MarginStocks.CollectionChanged += OnCollectionChanged;

            AddStockCommand = new RelayCommand(ExecuteAddStock);
            DeleteStockCommand = new RelayCommand(ExecuteDeleteStock);
            FetchAllPricesCommand = new RelayCommand(_ => ExecuteFetchAllPricesFireAndForget());

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
            // 現股
            CashStockValue = CashStocks.Sum(s => s.MarketValue);
            decimal cashStockCost = CashStocks.Sum(s => s.TotalCost);
            CashStockProfitLoss = CashStockValue - cashStockCost;
            CashStockProfitLossPercentage = cashStockCost != 0 ? (double)(CashStockProfitLoss / cashStockCost) : 0;

            // 融資
            MarginStockValue = MarginStocks.Sum(s => s.MarketValue);
            TotalMarginSelfFunded = MarginStocks.Sum(s => s.SelfFunded);
            TotalMarginProfitLoss = MarginStocks.Sum(s => s.ProfitLoss);
            TotalMarginProfitLossPercentage = TotalMarginSelfFunded != 0 ? (double)(TotalMarginProfitLoss / TotalMarginSelfFunded) : 0;

            // 全部股票合計
            TotalStockValue = CashStockValue + MarginStockValue;
            decimal marginStockCost = MarginStocks.Sum(s => s.TotalCost);
            decimal totalStockCost = cashStockCost + marginStockCost;
            TotalStockProfitLoss = CashStockProfitLoss + TotalMarginProfitLoss;
            TotalStockProfitLossPercentage = totalStockCost != 0 ? (double)(TotalStockProfitLoss / totalStockCost) : 0;

            // 曝險與槓桿
            TotalExposure = TotalStockValue;
            TotalCapital = CashStockValue + (TotalMarginSelfFunded + TotalMarginProfitLoss) + BankCash + StockSettlementAmount;
            Leverage = TotalCapital > 0 ? (double)(TotalExposure / TotalCapital) : 0;
        }

        // ==================== 股票 ====================

        private void ExecuteAddStock(object? obj)
        {
            StockItem newStock = new StockItem
            {
                StockCode = NewStockCode,
                Name = NewStockName,
                Shares = NewStockShares,
                EntryPrice = NewStockEntryPrice,
                CurrentPrice = NewStockCurrentPrice,
                StockType = NewStockType,
                MarginRatio = NewStockMarginRatio
            };
            StockItemViewModel vm = new StockItemViewModel(newStock);
            if (newStock.StockType == StockType.Margin)
            {
                MarginStocks.Add(vm);
            }
            else
            {
                CashStocks.Add(vm);
            }

            ClearStockInputs();
        }

        private void ClearStockInputs()
        {
            NewStockCode = string.Empty;
            NewStockName = string.Empty;
            NewStockShares = 0;
            NewStockEntryPrice = 0;
            NewStockCurrentPrice = 0;
            // 保留 NewStockType 和 NewStockMarginRatio 不重設
        }

        private void ExecuteDeleteStock(object? obj)
        {
            if (obj is StockItemViewModel stock)
            {
                if (!CashStocks.Remove(stock))
                {
                    MarginStocks.Remove(stock);
                }
            }
        }

        /// <summary>
        /// 股票批次匯入（含自動查詢股票名稱），回傳結果訊息
        /// </summary>
        public async Task<string> StockBatchImportAsync(List<StockBatchImportItem> items)
        {
            int successCount = 0;
            int processedCount = 0;
            Dictionary<string, string?> nameCache = new Dictionary<string, string?>();

            foreach (StockBatchImportItem item in items)
            {
                processedCount++;

                // 查詢股票名稱（同代號快取避免重複查詢）
                if (!nameCache.TryGetValue(item.StockCode, out string? stockName))
                {
                    PriceUpdateStatus = $"正在查詢 {item.StockCode} 的股票名稱... ({processedCount}/{items.Count})";
                    stockName = await _contractMappingService.GetStockNameAsync(item.StockCode);
                    nameCache[item.StockCode] = stockName;
                }

                StockItem stock = new StockItem
                {
                    StockCode = item.StockCode,
                    Name = stockName ?? string.Empty,
                    Shares = item.Shares,
                    EntryPrice = item.EntryPrice,
                    CurrentPrice = 0,
                    StockType = item.StockType,
                    MarginRatio = 0.6m
                };
                StockItemViewModel vm = new StockItemViewModel(stock);
                if (stock.StockType == StockType.Margin)
                {
                    MarginStocks.Add(vm);
                }
                else
                {
                    CashStocks.Add(vm);
                }
                successCount++;
            }

            string result = $"股票批次匯入完成：成功 {successCount} 筆";
            PriceUpdateStatus = result;
            return result;
        }

        // ==================== 持久化 ====================

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

            CashStocks.Clear();
            MarginStocks.Clear();
            foreach (StockItem stock in portfolio.Stocks)
            {
                StockItemViewModel vm = new StockItemViewModel(stock);
                if (stock.StockType == StockType.Margin)
                {
                    MarginStocks.Add(vm);
                }
                else
                {
                    CashStocks.Add(vm);
                }
            }

            RecalculateAll();
        }

        public void SaveData()
        {
            Portfolio portfolio = new Portfolio
            {
                BankCash = BankCash,
                StockSettlementAmount = StockSettlementAmount,
                Stocks = AllStocks.Select(vm => vm.Model).ToList()
            };
            _storageService.SavePortfolio(portfolio);
        }

        // ==================== 收盤價查詢 ====================

        private async void ExecuteFetchAllPricesFireAndForget()
        {
            try
            {
                await ExecuteFetchAllPricesAsync();
            }
            catch (Exception ex)
            {
                PriceUpdateStatus = $"更新收盤價時發生未預期的錯誤: {ex.Message}";
                IsUpdatingPrices = false;
            }
        }

        private async Task ExecuteFetchAllPricesAsync()
        {
            if (IsUpdatingPrices)
            {
                return;
            }

            IsUpdatingPrices = true;
            PriceUpdateStatus = "正在更新收盤價...";

            int stockSuccessCount = 0;
            int stockFailCount = 0;
            string twseDate = string.Empty;
            string tpexDate = string.Empty;

            List<string> errors = new List<string>();

            // === 更新股票（逐檔查詢 TWSE/TPEX）===
            Dictionary<string, StockPriceResult> stockPriceCache = new Dictionary<string, StockPriceResult>();

            foreach (StockItemViewModel stock in AllStocks)
            {
                string code = stock.StockCode?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                if (!stockPriceCache.TryGetValue(code, out StockPriceResult? cached))
                {
                    PriceUpdateStatus = $"正在查詢股票 {code}...";
                    cached = await _stockPriceService.GetClosingPriceAsync(code);

                    int retryCount = 0;
                    while (!cached.Success && retryCount < 2)
                    {
                        retryCount++;
                        int delayMs = retryCount * 2000;
                        PriceUpdateStatus = $"股票 {code} 查詢失敗，{delayMs / 1000} 秒後重試（第 {retryCount} 次）...";
                        await Task.Delay(delayMs);
                        cached = await _stockPriceService.GetClosingPriceAsync(code);
                    }

                    stockPriceCache[code] = cached;
                }
                if (cached.Success)
                {
                    stock.CurrentPrice = cached.ClosingPrice;
                    stockSuccessCount++;

                    if (cached.Source == "TWSE" && string.IsNullOrEmpty(twseDate) && !string.IsNullOrEmpty(cached.Date))
                    {
                        twseDate = cached.Date;
                    }
                    else if (cached.Source == "TPEX" && string.IsNullOrEmpty(tpexDate) && !string.IsNullOrEmpty(cached.Date))
                    {
                        tpexDate = cached.Date;
                    }
                }
                else
                {
                    stockFailCount++;
                    errors.Add($"{code}: {cached.ErrorMessage}");
                }
            }

            // === 組合狀態訊息 ===
            string status = $"更新股票 {stockSuccessCount} 筆";

            if (stockFailCount > 0)
            {
                status += $" ({stockFailCount} 筆失敗)";
            }

            List<string> dateParts = new List<string>();
            if (!string.IsNullOrEmpty(twseDate))
            {
                dateParts.Add($"上市:{twseDate}");
            }
            if (!string.IsNullOrEmpty(tpexDate))
            {
                dateParts.Add($"上櫃(OTC):{tpexDate}");
            }
            if (dateParts.Count > 0)
            {
                status += $"\n資料日期 — {string.Join(" / ", dateParts)}";
            }

            if (errors.Count > 0)
            {
                status += $" [{string.Join("; ", errors)}]";
            }

            PriceUpdateStatus = status;
            IsUpdatingPrices = false;
        }
    }
}
