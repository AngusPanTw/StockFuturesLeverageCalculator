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
        private readonly FuturesPriceService _futuresPriceService;
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

        /// <summary>
        /// 大型合約期貨庫存列表
        /// </summary>
        public ObservableCollection<FutureItemViewModel> LargeFutures { get; set; }

        /// <summary>
        /// 小型合約期貨庫存列表
        /// </summary>
        public ObservableCollection<FutureItemViewModel> SmallFutures { get; set; }

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

        // --- Add New Future ---
        private string _newUnderlyingStockCode = string.Empty;
        /// <summary>
        /// 新增期貨標的股票代號（如 "2330"）
        /// </summary>
        public string NewUnderlyingStockCode { get => _newUnderlyingStockCode; set { _newUnderlyingStockCode = value; OnPropertyChanged(); } }

        private string _newContractYear = string.Empty;
        /// <summary>
        /// 新增期貨合約年份（西元年後兩碼，如 "26"）
        /// </summary>
        public string NewContractYear { get => _newContractYear; set { _newContractYear = value; OnPropertyChanged(); } }

        private string _newContractMonthNum = string.Empty;
        /// <summary>
        /// 新增期貨合約月份（如 "03"）
        /// </summary>
        public string NewContractMonthNum { get => _newContractMonthNum; set { _newContractMonthNum = value; OnPropertyChanged(); } }

        /// <summary>
        /// 可選年份列表（西元年後兩碼）
        /// </summary>
        public List<string> AvailableYears { get; }

        /// <summary>
        /// 可選月份列表（01~12）
        /// </summary>
        public List<string> AvailableMonths { get; }

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
        public ICommand AddFutureCommand { get; }
        public ICommand DeleteFutureCommand { get; }
        public ICommand FetchAllPricesCommand { get; }

        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");
            _stockPriceService = new StockPriceService();
            _futuresPriceService = new FuturesPriceService();
            _contractMappingService = new ContractMappingService();

            // 啟動時預熱 TAIFEX 標的資料快取
            _ = _contractMappingService.PreloadCacheAsync();

            // 初始化年月選項
            int currentTwoDigitYear = DateTime.Now.Year % 100;
            AvailableYears = Enumerable.Range(currentTwoDigitYear, 5)
                .Select(y => y.ToString("D2"))
                .ToList();
            AvailableMonths = Enumerable.Range(1, 12)
                .Select(m => m.ToString("D2"))
                .ToList();

            NewContractYear = currentTwoDigitYear.ToString("D2");
            NewContractMonthNum = DateTime.Now.Month.ToString("D2");

            CashStocks = new ObservableCollection<StockItemViewModel>();
            CashStocks.CollectionChanged += OnCollectionChanged;

            MarginStocks = new ObservableCollection<StockItemViewModel>();
            MarginStocks.CollectionChanged += OnCollectionChanged;

            LargeFutures = new ObservableCollection<FutureItemViewModel>();
            LargeFutures.CollectionChanged += OnCollectionChanged;

            SmallFutures = new ObservableCollection<FutureItemViewModel>();
            SmallFutures.CollectionChanged += OnCollectionChanged;

            AddStockCommand = new RelayCommand(ExecuteAddStock);
            DeleteStockCommand = new RelayCommand(ExecuteDeleteStock);
            AddFutureCommand = new RelayCommand(_ => ExecuteAddFutureFireAndForget());
            DeleteFutureCommand = new RelayCommand(ExecuteDeleteFuture);
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

            // 期貨
            TotalFuturesExposure = AllFutures.Sum(f => f.Exposure);
            TotalFuturesProfitLoss = AllFutures.Sum(f => f.ProfitLoss);
            decimal totalFuturesCostValue = AllFutures.Sum(f => f.CostPrice * f.SharesPerLot * f.Lots);
            TotalFuturesProfitLossPercentage = totalFuturesCostValue != 0 ? (double)(TotalFuturesProfitLoss / totalFuturesCostValue) : 0;

            // 曝險與槓桿
            TotalExposure = TotalStockValue + TotalFuturesExposure;
            TotalCapital = CashStockValue + (TotalMarginSelfFunded + TotalMarginProfitLoss) + BankCash + StockSettlementAmount + FuturesEquity;
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

        // ==================== 期貨 ====================

        /// <summary>
        /// 驗證新增期貨的輸入欄位，回傳 null 表示通過，否則回傳錯誤訊息
        /// </summary>
        public string? ValidateNewFutureInputs()
        {
            if (string.IsNullOrWhiteSpace(NewUnderlyingStockCode))
            {
                return "請輸入股票代號";
            }
            if (string.IsNullOrWhiteSpace(NewContractYear) || string.IsNullOrWhiteSpace(NewContractMonthNum))
            {
                return "請選擇合約年月";
            }
            if (NewFutureLots <= 0)
            {
                return "口數必須大於 0";
            }
            return null;
        }

        /// <summary>
        /// 新增期貨（含 API 查詢），回傳 null 表示成功，否則回傳錯誤訊息
        /// </summary>
        public async Task<string?> AddFutureAsync()
        {
            string stockCode = NewUnderlyingStockCode.Trim();
            string month = NewContractYear + NewContractMonthNum;
            string contractType = NewFutureIsSmallContract ? "小型" : "大型";

            PriceUpdateStatus = $"正在查詢 {stockCode} 的{contractType}合約代號...";

            ContractInfo? contractInfo = await _contractMappingService.GetContractInfoAsync(stockCode, NewFutureIsSmallContract);

            if (contractInfo == null)
            {
                PriceUpdateStatus = string.Empty;
                return $"無法由「{stockCode}」查找到任何{contractType}股票期貨資料\n\n請確認股票代號是否正確，或該標的是否有{contractType}合約";
            }

            string futureStockCode = contractInfo.ContractCode + NormalizeContractMonth(month);

            PriceUpdateStatus = $"已查詢到合約: {contractInfo.ContractName} ({futureStockCode})";

            FutureItem newFuture = new FutureItem
            {
                UnderlyingStockCode = stockCode,
                ContractMonth = month,
                StockCode = futureStockCode,
                Name = contractInfo.ContractName,
                Lots = NewFutureLots,
                Position = NewFuturePosition,
                CostPrice = NewFutureCostPrice,
                CurrentPrice = NewFutureCurrentPrice,
                IsSmallContract = NewFutureIsSmallContract
            };

            FutureItemViewModel vm = new FutureItemViewModel(newFuture);
            if (newFuture.IsSmallContract)
            {
                SmallFutures.Add(vm);
            }
            else
            {
                LargeFutures.Add(vm);
            }

            ClearFutureInputs();
            return null;
        }

        private async void ExecuteAddFutureFireAndForget()
        {
            try
            {
                string? error = ValidateNewFutureInputs();
                if (error != null)
                {
                    PriceUpdateStatus = error;
                    return;
                }

                string? addError = await AddFutureAsync();
                if (addError != null)
                {
                    PriceUpdateStatus = addError;
                }
            }
            catch (Exception ex)
            {
                PriceUpdateStatus = $"新增期貨時發生錯誤: {ex.Message}";
            }
        }

        /// <summary>
        /// 批次匯入期貨項目，回傳結果訊息
        /// </summary>
        public async Task<string> FutureBatchImportAsync(List<BatchImportItem> items)
        {
            int successCount = 0;
            List<string> failures = new List<string>();

            foreach (BatchImportItem item in items)
            {
                string contractType = item.IsSmallContract ? "小型" : "大型";
                PriceUpdateStatus = $"正在查詢 {item.StockCode} 的{contractType}合約代號... ({successCount + failures.Count + 1}/{items.Count})";

                ContractInfo? contractInfo = await _contractMappingService.GetContractInfoAsync(item.StockCode, item.IsSmallContract);
                if (contractInfo == null)
                {
                    failures.Add($"{item.StockCode}({contractType}): 查無合約");
                    continue;
                }

                string futureStockCode = contractInfo.ContractCode + NormalizeContractMonth(item.ContractMonth);

                FutureItem newFuture = new FutureItem
                {
                    UnderlyingStockCode = item.StockCode,
                    ContractMonth = item.ContractMonth,
                    StockCode = futureStockCode,
                    Name = contractInfo.ContractName,
                    Lots = item.Lots,
                    Position = PositionType.Long,
                    CostPrice = item.CostPrice,
                    CurrentPrice = 0,
                    IsSmallContract = item.IsSmallContract
                };

                FutureItemViewModel vm = new FutureItemViewModel(newFuture);
                if (newFuture.IsSmallContract)
                {
                    SmallFutures.Add(vm);
                }
                else
                {
                    LargeFutures.Add(vm);
                }
                successCount++;
            }

            string result = $"批次匯入完成：成功 {successCount} 筆";
            if (failures.Count > 0)
            {
                result += $"，失敗 {failures.Count} 筆\n" + string.Join("\n", failures);
            }
            PriceUpdateStatus = result;
            return result;
        }

        private void ClearFutureInputs()
        {
            NewUnderlyingStockCode = string.Empty;
            NewFutureLots = 0;
            NewFutureCostPrice = 0;
            NewFutureCurrentPrice = 0;
            NewFutureIsSmallContract = false;
            // 保留 NewFuturePosition、NewContractYear、NewContractMonthNum 不重設
        }

        private void ExecuteDeleteFuture(object? obj)
        {
            if (obj is FutureItemViewModel future)
            {
                if (!LargeFutures.Remove(future))
                {
                    SmallFutures.Remove(future);
                }
            }
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
            FuturesEquity = portfolio.FuturesEquity;

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

            LargeFutures.Clear();
            SmallFutures.Clear();
            foreach (FutureItem future in portfolio.Futures)
            {
                FutureItemViewModel vm = new FutureItemViewModel(future);
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
            Portfolio portfolio = new Portfolio
            {
                BankCash = BankCash,
                StockSettlementAmount = StockSettlementAmount,
                FuturesEquity = FuturesEquity,
                Stocks = AllStocks.Select(vm => vm.Model).ToList(),
                Futures = AllFutures.Select(vm => vm.Model).ToList()
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
            int futuresSuccessCount = 0;
            int futuresFailCount = 0;
            string twseDate = string.Empty;
            string tpexDate = string.Empty;
            string futuresDate = string.Empty;

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

            // === 補查期貨合約代號（StockCode 為空時用 ContractMappingService 查詢）===
            foreach (FutureItemViewModel future in AllFutures)
            {
                if (!string.IsNullOrEmpty(future.StockCode?.Trim()))
                {
                    continue;
                }

                string underlyingCode = future.UnderlyingStockCode?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(underlyingCode))
                {
                    continue;
                }

                PriceUpdateStatus = $"正在查詢 {underlyingCode} 的合約代號...";
                ContractInfo? info = await _contractMappingService.GetContractInfoAsync(underlyingCode, future.IsSmallContract);
                if (info != null)
                {
                    future.StockCode = info.ContractCode + NormalizeContractMonth(future.ContractMonth);
                    future.Name = info.ContractName;
                }
            }

            // === 更新期貨（一次呼叫 TAIFEX API，批次比對）===
            PriceUpdateStatus = "正在查詢期貨行情...";
            FuturesBatchResult futuresBatch = await _futuresPriceService.GetAllFuturesPricesAsync();

            if (futuresBatch.Success)
            {
                foreach (FutureItemViewModel future in AllFutures)
                {
                    string code = NormalizeFuturesCode(future.StockCode?.Trim() ?? string.Empty);
                    if (string.IsNullOrEmpty(code))
                    {
                        continue;
                    }

                    if (futuresBatch.Prices.TryGetValue(code, out FuturesPriceResult? priceResult))
                    {
                        future.CurrentPrice = priceResult.ClosingPrice;
                        futuresSuccessCount++;

                        if (string.IsNullOrEmpty(futuresDate) && !string.IsNullOrEmpty(priceResult.Date))
                        {
                            futuresDate = priceResult.Date;
                        }
                    }
                    else
                    {
                        futuresFailCount++;
                        errors.Add($"[{code}]: 查無期貨行情");
                    }
                }
            }
            else
            {
                int futuresTotal = AllFutures.Count(f => !string.IsNullOrEmpty(f.StockCode?.Trim()));
                futuresFailCount = futuresTotal;
                errors.Add(futuresBatch.ErrorMessage);
            }

            // === 組合狀態訊息 ===
            string status = $"更新股票 {stockSuccessCount} 筆, 更新股期 {futuresSuccessCount} 筆";

            if (stockFailCount > 0 || futuresFailCount > 0)
            {
                List<string> failParts = new List<string>();
                if (stockFailCount > 0)
                {
                    failParts.Add($"股票{stockFailCount}筆");
                }
                if (futuresFailCount > 0)
                {
                    failParts.Add($"股期{futuresFailCount}筆");
                }
                status += $" ({string.Join(", ", failParts)}失敗)";
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
            string futuresDateDisplay = FormatTaifexDate(futuresDate);
            if (!string.IsNullOrEmpty(futuresDateDisplay))
            {
                dateParts.Add($"股期:{futuresDateDisplay}");
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

        // ==================== 輔助方法 ====================

        /// <summary>
        /// 將合約月份正規化為 6 碼格式（如 "2603" → "202603"）
        /// </summary>
        private static string NormalizeContractMonth(string month)
        {
            if (month.Length == 4)
            {
                return "20" + month;
            }
            return month;
        }

        /// <summary>
        /// 將使用者輸入的期貨代碼正規化為 API 格式。
        /// 支援短格式 "KUF2603" → "KUF202603"，也接受完整格式 "KUF202603"。
        /// </summary>
        private static string NormalizeFuturesCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            int digitStart = -1;
            for (int i = 0; i < code.Length; i++)
            {
                if (char.IsDigit(code[i]))
                {
                    digitStart = i;
                    break;
                }
            }

            if (digitStart <= 0)
            {
                return code;
            }

            string contract = code[..digitStart];
            string month = code[digitStart..];

            if (month.Length == 4)
            {
                month = "20" + month;
            }

            return contract + month;
        }

        /// <summary>
        /// 將 TAIFEX 日期格式 "20260224" 轉為 "2026/02/24"
        /// </summary>
        private static string FormatTaifexDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate) || rawDate.Length != 8)
            {
                return string.Empty;
            }
            return $"{rawDate[..4]}/{rawDate[4..6]}/{rawDate[6..8]}";
        }
    }
}
