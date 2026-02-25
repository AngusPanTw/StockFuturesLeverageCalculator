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
        /// 大台期貨庫存列表
        /// </summary>
        public ObservableCollection<FutureItemViewModel> LargeFutures { get; set; }
        
        /// <summary>
        /// 小台期貨庫存列表
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
        private string _newStockCode = "";
        /// <summary>
        /// 新增股票代號
        /// </summary>
        public string NewStockCode { get => _newStockCode; set { _newStockCode = value; OnPropertyChanged(); } }
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
        private string _newFutureStockCode = "";
        /// <summary>
        /// 新增期貨標的代號
        /// </summary>
        public string NewFutureStockCode { get => _newFutureStockCode; set { _newFutureStockCode = value; OnPropertyChanged(); } }
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

        private decimal _totalMarginLoan;
        /// <summary>
        /// 融資借款總額
        /// </summary>
        public decimal TotalMarginLoan { get => _totalMarginLoan; private set { _totalMarginLoan = value; OnPropertyChanged(); } }

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
        private string _riskStatus = "";
        /// <summary>
        /// 風險狀態描述
        /// </summary>
        public string RiskStatus { get => _riskStatus; private set { _riskStatus = value; OnPropertyChanged(); } }
        
        // --- Price Update ---
        private string _priceUpdateStatus = "";
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
        /// <summary>
        /// 一鍵更新所有庫存收盤價
        /// </summary>
        public ICommand FetchAllPricesCommand { get; }

        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");
            _stockPriceService = new StockPriceService();
            _futuresPriceService = new FuturesPriceService();

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
            AddFutureCommand = new RelayCommand(ExecuteAddFuture);
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
            TotalMarginLoan = MarginStocks.Sum(s => s.LoanAmount);
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
            // 淨資產 = 現股市值 + (融資自備款 + 融資損益) + 可用資金 + 交割款 + 期貨權益金
            TotalCapital = CashStockValue + (TotalMarginSelfFunded + TotalMarginProfitLoss) + BankCash + StockSettlementAmount + FuturesEquity;

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
            NewStockCode = "";
            NewStockName = "";
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
        
        private void ExecuteAddFuture(object? obj)
        {
            FutureItem newFuture = new FutureItem
            {
                StockCode = NewFutureStockCode,
                Name = NewFutureName,
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
        }

        private void ClearFutureInputs()
        {
            NewFutureStockCode = "";
            NewFutureName = "";
            NewFutureLots = 0;
            NewFutureCostPrice = 0;
            NewFutureCurrentPrice = 0;
            NewFutureIsSmallContract = false;
            // 保留 NewFuturePosition 不重設
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

        /// <summary>
        /// 非同步命令的進入點，捕捉未處理的例外避免應用程式崩潰
        /// </summary>
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

        /// <summary>
        /// 一鍵更新所有庫存的收盤價（股票 + 股票期貨）
        /// </summary>
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
            string twseDate = "";
            string tpexDate = "";
            string futuresDate = "";

            List<string> errors = new List<string>();

            // === 更新股票（逐檔查詢 TWSE/TPEX）===
            Dictionary<string, StockPriceResult> stockPriceCache = new Dictionary<string, StockPriceResult>();

            foreach (StockItemViewModel stock in AllStocks)
            {
                string code = stock.StockCode?.Trim() ?? "";
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                if (!stockPriceCache.TryGetValue(code, out StockPriceResult? cached))
                {
                    PriceUpdateStatus = $"正在查詢股票 {code}...";
                    cached = await _stockPriceService.GetClosingPriceAsync(code);

                    // 查詢失敗時重試（最多 2 次，遞增等待）
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

            // === 更新期貨（一次呼叫 TAIFEX API，批次比對）===
            PriceUpdateStatus = "正在查詢期貨行情...";
            FuturesBatchResult futuresBatch = await _futuresPriceService.GetAllFuturesPricesAsync();

            if (futuresBatch.Success)
            {
                foreach (FutureItemViewModel future in AllFutures)
                {
                    string code = NormalizeFuturesCode(future.StockCode?.Trim() ?? "");
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
                // API 呼叫本身失敗，所有期貨都算失敗
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

            // 日期：分開顯示上市(TWSE)、上櫃(OTC)、股期(TAIFEX)
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

        /// <summary>
        /// 將使用者輸入的期貨代碼正規化為 API 格式。
        /// 支援短格式 "KUF2603" → "KUF202603"，也接受完整格式 "KUF202603"。
        /// </summary>
        private static string NormalizeFuturesCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            // 找出第一個數字的位置（Contract 部分為英文，剩下的是月份）
            int digitStart = -1;
            for (int i = 0; i < code.Length; i++)
            {
                if (char.IsDigit(code[i]))
                {
                    digitStart = i;
                    break;
                }
            }

            // 沒有數字或全是數字（無 Contract 前綴），直接回傳原始值
            if (digitStart <= 0)
            {
                return code;
            }

            string contract = code[..digitStart];
            string month = code[digitStart..];

            // 短格式 4 碼 "2603" → 補成 "202603"
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
                return "";
            }
            return $"{rawDate[..4]}/{rawDate[4..6]}/{rawDate[6..8]}";
        }
    }
}
