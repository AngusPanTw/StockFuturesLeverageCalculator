using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using LeverageCalculator.Models;
using LeverageCalculator.Services;

namespace LeverageCalculator.ViewModels
{
    /// <summary>
    /// 主視窗 ViewModel，管理股票期貨庫存、分群與市值計算
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly PortfolioStorageService _storageService;
        private readonly FuturesPriceService _futuresPriceService;
        private readonly ContractMappingService _contractMappingService;

        // --- Collections ---
        /// <summary>
        /// 所有期貨庫存（合併大型/小型為單一列表）
        /// </summary>
        public ObservableCollection<FutureItemViewModel> AllFutures { get; }

        /// <summary>
        /// 分群列表
        /// </summary>
        public ObservableCollection<FutureGroupViewModel> FutureGroups { get; }

        /// <summary>
        /// 未分群的期貨列表（計算屬性）
        /// </summary>
        public ObservableCollection<FutureItemViewModel> UngroupedFutures { get; }

        // --- Add New Future ---
        private string _newUnderlyingStockCode = string.Empty;
        public string NewUnderlyingStockCode { get => _newUnderlyingStockCode; set { _newUnderlyingStockCode = value; OnPropertyChanged(); } }

        /// <summary>
        /// 可選年份列表（民國年後兩碼）
        /// </summary>
        public List<string> AvailableYears { get; }

        /// <summary>
        /// 可選月份列表（01~12）
        /// </summary>
        public List<string> AvailableMonths { get; }

        private string _newContractYear = string.Empty;
        public string NewContractYear { get => _newContractYear; set { _newContractYear = value; OnPropertyChanged(); } }

        private string _newContractMonthNum = string.Empty;
        public string NewContractMonthNum { get => _newContractMonthNum; set { _newContractMonthNum = value; OnPropertyChanged(); } }

        private int _newFutureLots;
        public int NewFutureLots { get => _newFutureLots; set { _newFutureLots = value; OnPropertyChanged(); } }

        private PositionType _newFuturePosition = PositionType.Long;
        public PositionType NewFuturePosition { get => _newFuturePosition; set { _newFuturePosition = value; OnPropertyChanged(); } }

        private bool _newFutureIsSmallContract;
        public bool NewFutureIsSmallContract { get => _newFutureIsSmallContract; set { _newFutureIsSmallContract = value; OnPropertyChanged(); } }

        // --- Calculated Results ---
        private decimal _totalFuturesExposure;
        public decimal TotalFuturesExposure { get => _totalFuturesExposure; private set { _totalFuturesExposure = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalFuturesExposureInWan)); } }

        public string TotalFuturesExposureInWan => FormatWan(TotalFuturesExposure);

        private int _totalLots;
        public int TotalLots { get => _totalLots; private set { _totalLots = value; OnPropertyChanged(); } }

        // --- Price Update ---
        private string _priceUpdateStatus = string.Empty;
        public string PriceUpdateStatus { get => _priceUpdateStatus; set { _priceUpdateStatus = value; OnPropertyChanged(); } }

        private bool _isUpdatingPrices;
        public bool IsUpdatingPrices { get => _isUpdatingPrices; set { _isUpdatingPrices = value; OnPropertyChanged(); } }

        // --- Group Management ---
        private string _newGroupName = string.Empty;
        public string NewGroupName { get => _newGroupName; set { _newGroupName = value; OnPropertyChanged(); } }

        // --- Commands ---
        public ICommand AddFutureCommand { get; }
        public ICommand DeleteFutureCommand { get; }
        public ICommand FetchAllPricesCommand { get; }
        public ICommand CreateGroupCommand { get; }
        public ICommand DeleteGroupCommand { get; }
        public ICommand MoveToGroupCommand { get; }
        public ICommand RemoveFromGroupCommand { get; }

        public MainViewModel()
        {
            _storageService = new PortfolioStorageService("portfolio.json");
            _futuresPriceService = new FuturesPriceService();
            _contractMappingService = new ContractMappingService();

            // 初始化年月選項（西元年後兩碼，如 "26" 代表 2026）
            int currentTwoDigitYear = DateTime.Now.Year % 100;
            AvailableYears = Enumerable.Range(currentTwoDigitYear, 5)
                .Select(y => y.ToString())
                .ToList();
            AvailableMonths = Enumerable.Range(1, 12)
                .Select(m => m.ToString("D2"))
                .ToList();

            // 預設為當前年月
            NewContractYear = currentTwoDigitYear.ToString();
            NewContractMonthNum = DateTime.Now.Month.ToString("D2");

            AllFutures = new ObservableCollection<FutureItemViewModel>();
            AllFutures.CollectionChanged += OnCollectionChanged;

            FutureGroups = new ObservableCollection<FutureGroupViewModel>();
            UngroupedFutures = new ObservableCollection<FutureItemViewModel>();

            AddFutureCommand = new RelayCommand(_ => ExecuteAddFutureFireAndForget());
            DeleteFutureCommand = new RelayCommand(ExecuteDeleteFuture);
            FetchAllPricesCommand = new RelayCommand(_ => ExecuteFetchAllPricesFireAndForget());
            CreateGroupCommand = new RelayCommand(ExecuteCreateGroup);
            DeleteGroupCommand = new RelayCommand(ExecuteDeleteGroup);
            MoveToGroupCommand = new RelayCommand(ExecuteMoveToGroup);
            RemoveFromGroupCommand = new RelayCommand(ExecuteRemoveFromGroup);

            LoadData();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Reset 時 OldItems 為 null，無法逐一取消訂閱
                // 由呼叫端（如 LoadData）負責在 Clear 前處理
            }
            else
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
            }

            if (!_suppressRecalculation)
            {
                RecalculateAll();
            }
        }

        /// <summary>
        /// 為 true 時暫停自動重算，用於批次操作期間避免過度觸發
        /// </summary>
        private bool _suppressRecalculation;

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_suppressRecalculation)
            {
                RecalculateAll();
            }
        }

        private void RecalculateAll()
        {
            TotalFuturesExposure = AllFutures.Sum(f => f.Exposure);
            TotalLots = AllFutures.Sum(f => f.Lots);
            RefreshGroups();
        }

        /// <summary>
        /// 重新整理分群與未分群列表
        /// </summary>
        private void RefreshGroups()
        {
            // 更新每個群組的項目與計算
            foreach (FutureGroupViewModel group in FutureGroups)
            {
                group.Items.Clear();
                foreach (FutureItemViewModel future in AllFutures.Where(f => f.GroupName == group.GroupName))
                {
                    group.Items.Add(future);
                }
                group.NotifyRecalculate();
            }

            // 更新未分群列表
            UngroupedFutures.Clear();
            foreach (FutureItemViewModel future in AllFutures.Where(f => string.IsNullOrEmpty(f.GroupName)))
            {
                UngroupedFutures.Add(future);
            }
        }

        // --- Add Future (with API lookup) ---

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
                return "請選擇合約年份與月份";
            }
            if (NewFutureLots <= 0)
            {
                return "請輸入口數（須大於 0）";
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

            FutureItemViewModel vm = CreateFutureItemViewModel(
                stockCode, month, futureStockCode, contractInfo.ContractName,
                NewFutureLots, NewFuturePosition, NewFutureIsSmallContract);
            AllFutures.Add(vm);

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
        public async Task<string> BatchImportAsync(List<BatchImportItem> items)
        {
            int successCount = 0;
            List<string> failures = new List<string>();

            _suppressRecalculation = true;

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

                FutureItemViewModel vm = CreateFutureItemViewModel(
                    item.StockCode, item.ContractMonth, futureStockCode, contractInfo.ContractName,
                    item.Lots, PositionType.Long, item.IsSmallContract);
                AllFutures.Add(vm);
                successCount++;
            }

            _suppressRecalculation = false;
            RecalculateAll();

            string result = $"批次匯入完成：{successCount} 筆成功";
            if (failures.Count > 0)
            {
                result += $"，{failures.Count} 筆失敗\n{string.Join("\n", failures)}";
            }

            PriceUpdateStatus = result;
            return result;
        }

        private void ClearFutureInputs()
        {
            NewUnderlyingStockCode = string.Empty;
            NewFutureLots = 0;
            // 保留年月、多空、大小型不重設，方便連續新增同月份合約
        }

        private void ExecuteDeleteFuture(object? obj)
        {
            if (obj is FutureItemViewModel future)
            {
                AllFutures.Remove(future);
            }
        }

        // --- Group Management ---
        private void ExecuteCreateGroup(object? obj)
        {
            string name = NewGroupName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (FutureGroups.Any(g => g.GroupName == name))
            {
                return;
            }

            FutureGroupViewModel group = new FutureGroupViewModel(name);
            FutureGroups.Add(group);
            NewGroupName = string.Empty;
            RefreshGroups();
        }

        private void ExecuteDeleteGroup(object? obj)
        {
            if (obj is FutureGroupViewModel group)
            {
                // 將群組內的期貨移回未分群
                foreach (FutureItemViewModel future in AllFutures.Where(f => f.GroupName == group.GroupName))
                {
                    future.GroupName = string.Empty;
                }
                FutureGroups.Remove(group);
                RefreshGroups();
            }
        }

        /// <summary>
        /// 將期貨移入指定群組。CommandParameter 為 Tuple(FutureItemViewModel, string groupName)
        /// </summary>
        public void MoveItemToGroup(FutureItemViewModel future, string groupName)
        {
            future.GroupName = groupName;
            RefreshGroups();
        }

        private void ExecuteMoveToGroup(object? obj)
        {
            if (obj is Tuple<FutureItemViewModel, string> tuple)
            {
                MoveItemToGroup(tuple.Item1, tuple.Item2);
            }
        }

        /// <summary>
        /// 將期貨從群組移除（回到未分群）
        /// </summary>
        public void RemoveItemFromGroup(FutureItemViewModel future)
        {
            future.GroupName = string.Empty;
            RefreshGroups();
        }

        private void ExecuteRemoveFromGroup(object? obj)
        {
            if (obj is FutureItemViewModel future)
            {
                RemoveItemFromGroup(future);
            }
        }

        // --- Data Persistence ---
        private void LoadData()
        {
            Portfolio? portfolio = _storageService.LoadPortfolio();
            if (portfolio == null)
            {
                RecalculateAll();
                return;
            }

            _suppressRecalculation = true;

            // 取消現有項目的事件訂閱後再 Clear
            foreach (FutureItemViewModel existing in AllFutures)
            {
                existing.PropertyChanged -= OnItemPropertyChanged;
            }

            // 先載入群組名稱
            FutureGroups.Clear();
            foreach (string groupName in portfolio.GroupNames)
            {
                FutureGroups.Add(new FutureGroupViewModel(groupName));
            }

            // 載入期貨
            AllFutures.Clear();
            foreach (FutureItem future in portfolio.Futures)
            {
                FutureItemViewModel vm = new FutureItemViewModel(future);
                AllFutures.Add(vm);
            }

            _suppressRecalculation = false;
            RecalculateAll();
        }

        public void SaveData()
        {
            Portfolio portfolio = new Portfolio
            {
                Futures = AllFutures.Select(vm => vm.Model).ToList(),
                GroupNames = FutureGroups.Select(g => g.GroupName).ToList()
            };
            _storageService.SavePortfolio(portfolio);
        }

        // --- Price Update ---
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

            int futuresSuccessCount = 0;
            int futuresFailCount = 0;
            string futuresDate = string.Empty;
            List<string> errors = new List<string>();

            // 先確保所有期貨都有合約代號（呼叫 ContractMappingService）
            PriceUpdateStatus = "正在查詢合約代號對照表...";
            foreach (FutureItemViewModel future in AllFutures)
            {
                string underlyingCode = future.UnderlyingStockCode?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(underlyingCode) || !string.IsNullOrEmpty(future.StockCode))
                {
                    continue;
                }

                ContractInfo? info = await _contractMappingService.GetContractInfoAsync(underlyingCode, future.IsSmallContract);
                if (info != null)
                {
                    future.StockCode = info.ContractCode + NormalizeContractMonth(future.ContractMonth);
                    if (string.IsNullOrEmpty(future.Name) || future.Name.StartsWith("未知"))
                    {
                        future.Name = info.ContractName;
                    }
                }
            }

            // 呼叫 TAIFEX DailyMarketReportFut API 取得收盤價
            PriceUpdateStatus = "正在查詢期貨行情...";
            FuturesBatchResult futuresBatch = await _futuresPriceService.GetAllFuturesPricesAsync();

            if (futuresBatch.Success)
            {
                _suppressRecalculation = true;

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

                _suppressRecalculation = false;
                RecalculateAll();
            }
            else
            {
                int futuresTotal = AllFutures.Count(f => !string.IsNullOrEmpty(f.StockCode?.Trim()));
                futuresFailCount = futuresTotal;
                errors.Add(futuresBatch.ErrorMessage);
            }

            // 組合狀態訊息
            string status = $"已更新 {futuresSuccessCount} 檔股票期貨";

            if (futuresFailCount > 0)
            {
                status += $" ({futuresFailCount}檔失敗)";
            }

            string futuresDateDisplay = FormatTaifexDate(futuresDate);
            if (!string.IsNullOrEmpty(futuresDateDisplay))
            {
                status += $"\n資料日期: {futuresDateDisplay}";
            }

            if (errors.Count > 0)
            {
                status += $"\n{string.Join("; ", errors)}";
            }

            PriceUpdateStatus = status;
            IsUpdatingPrices = false;
        }

        /// <summary>
        /// 將 4 碼合約月份展開為 6 碼（如 "2603" → "202603"），已為 6 碼則原樣回傳
        /// </summary>
        private static string NormalizeContractMonth(string month)
        {
            return month.Length == 4 ? "20" + month : month;
        }

        /// <summary>
        /// 建立 FutureItemViewModel，統一 FutureItem 的建構邏輯
        /// </summary>
        private static FutureItemViewModel CreateFutureItemViewModel(
            string underlyingStockCode, string contractMonth, string stockCode,
            string name, int lots, PositionType position, bool isSmallContract)
        {
            FutureItem newFuture = new FutureItem
            {
                UnderlyingStockCode = underlyingStockCode,
                ContractMonth = contractMonth,
                StockCode = stockCode,
                Name = name,
                Lots = lots,
                Position = position,
                IsSmallContract = isSmallContract
            };
            return new FutureItemViewModel(newFuture);
        }

        /// <summary>
        /// 將使用者輸入的期貨代碼正規化為 API 格式。
        /// 支援短格式 "KUF2603" → "KUF202603"。
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
