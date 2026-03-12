using System.Net.Http;
using System.Text.Json;

namespace LeverageCalculator.Services
{
    /// <summary>
    /// 合約代號查詢結果
    /// </summary>
    public class ContractInfo
    {
        /// <summary>
        /// 合約代碼（如 "CDF"）
        /// </summary>
        public string ContractCode { get; init; } = string.Empty;

        /// <summary>
        /// 合約名稱（如 "台積電期貨"）
        /// </summary>
        public string ContractName { get; init; } = string.Empty;
    }

    /// <summary>
    /// 股票資訊統一查詢服務，管理三份快取：
    /// - TSE（上市）股票代號→名稱
    /// - TPEX（上櫃）股票代號→名稱
    /// - TAIFEX 股票期貨合約對應
    ///
    /// 啟動時三份快取並行預熱（Task.WhenAll），後續查詢直接查記憶體。
    /// </summary>
    public class StockInfoService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private const string TseApiUrl =
            "https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_AVG_ALL";
        private const string TpexApiUrl =
            "https://www.tpex.org.tw/openapi/v1/tpex_mainboard_daily_close_quotes";
        private const string TaifexApiUrl =
            "https://openapi.taifex.com.tw/v1/SingleStockFuturesMargining";

        /// <summary>
        /// 上市：Code → Name
        /// </summary>
        private Dictionary<string, string>? _tseCache;

        /// <summary>
        /// 上櫃：SecuritiesCompanyCode → CompanyName
        /// </summary>
        private Dictionary<string, string>? _tpexCache;

        /// <summary>
        /// TAIFEX 股票期貨合約對應：(股票代號, 是否小型) → ContractInfo
        /// </summary>
        private Dictionary<(string StockCode, bool IsSmall), ContractInfo>? _taifexCache;

        /// <summary>
        /// 防止並行初始化快取的鎖
        /// </summary>
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        static StockInfoService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // ==================== 快取載入 ====================

        /// <summary>
        /// 確保三份快取均已載入（執行緒安全）。
        /// 三個 API 彼此獨立，使用 Task.WhenAll 並行請求。
        /// </summary>
        private async Task EnsureCacheLoadedAsync()
        {
            if (_tseCache != null && _tpexCache != null && _taifexCache != null)
            {
                return;
            }

            await _cacheLock.WaitAsync();
            try
            {
                if (_tseCache != null && _tpexCache != null && _taifexCache != null)
                {
                    return;
                }

                Task<Dictionary<string, string>>? tseTask = null;
                Task<Dictionary<string, string>>? tpexTask = null;
                Task<Dictionary<(string, bool), ContractInfo>>? taifexTask = null;

                if (_tseCache == null) tseTask = LoadTseCacheAsync();
                if (_tpexCache == null) tpexTask = LoadTpexCacheAsync();
                if (_taifexCache == null) taifexTask = LoadTaifexCacheAsync();

                List<Task> pending = new List<Task>();
                if (tseTask != null) pending.Add(tseTask);
                if (tpexTask != null) pending.Add(tpexTask);
                if (taifexTask != null) pending.Add(taifexTask);

                await Task.WhenAll(pending);

                if (tseTask != null) _tseCache = tseTask.Result;
                if (tpexTask != null) _tpexCache = tpexTask.Result;
                if (taifexTask != null) _taifexCache = taifexTask.Result;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// 從 TSE API 載入上市股票代號→名稱對應表。
        /// </summary>
        private static async Task<Dictionary<string, string>> LoadTseCacheAsync()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string json = await _httpClient.GetStringAsync(TseApiUrl);
            List<JsonElement>? items = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (items == null)
            {
                return result;
            }

            foreach (JsonElement item in items)
            {
                if (!item.TryGetProperty("Code", out JsonElement codeEl)
                    || !item.TryGetProperty("Name", out JsonElement nameEl))
                {
                    continue;
                }

                string code = codeEl.GetString() ?? string.Empty;
                string name = nameEl.GetString() ?? string.Empty;

                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                {
                    result[code] = name;
                }
            }

            return result;
        }

        /// <summary>
        /// 從 TPEX API 載入上櫃股票代號→名稱對應表。
        /// </summary>
        private static async Task<Dictionary<string, string>> LoadTpexCacheAsync()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string json = await _httpClient.GetStringAsync(TpexApiUrl);
            List<JsonElement>? items = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (items == null)
            {
                return result;
            }

            foreach (JsonElement item in items)
            {
                if (!item.TryGetProperty("SecuritiesCompanyCode", out JsonElement codeEl)
                    || !item.TryGetProperty("CompanyName", out JsonElement nameEl))
                {
                    continue;
                }

                string code = codeEl.GetString() ?? string.Empty;
                string name = nameEl.GetString() ?? string.Empty;

                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                {
                    result[code] = name;
                }
            }

            return result;
        }

        /// <summary>
        /// 從 TAIFEX API 載入股票期貨合約對應資料，解析為 (股票代號, 是否小型) → ContractInfo。
        /// </summary>
        private static async Task<Dictionary<(string, bool), ContractInfo>> LoadTaifexCacheAsync()
        {
            Dictionary<(string, bool), ContractInfo> result = new();
            string json = await _httpClient.GetStringAsync(TaifexApiUrl);
            List<JsonElement>? items = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (items == null)
            {
                return result;
            }

            foreach (JsonElement item in items)
            {
                if (!item.TryGetProperty("UnderlyingSecurityCode", out JsonElement codeEl)
                    || !item.TryGetProperty("ContractName", out JsonElement nameEl)
                    || !item.TryGetProperty("Contract", out JsonElement contractEl))
                {
                    continue;
                }

                string stockCode = codeEl.GetString() ?? string.Empty;
                string contractName = nameEl.GetString() ?? string.Empty;
                string contractCode = contractEl.GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(stockCode) || string.IsNullOrEmpty(contractCode))
                {
                    continue;
                }

                bool isSmall = contractName.Contains("小型");
                result[(stockCode, isSmall)] = new ContractInfo
                {
                    ContractCode = contractCode,
                    ContractName = contractName
                };
            }

            return result;
        }

        /// <summary>
        /// 預熱全部快取，啟動時 fire-and-forget 呼叫。
        /// 內部已有 try-catch，不會拋出例外。
        /// </summary>
        public async Task PreloadCacheAsync()
        {
            try
            {
                await EnsureCacheLoadedAsync();
            }
            catch
            {
                // 預熱失敗不影響後續功能，後續查詢會再次嘗試載入
            }
        }

        // ==================== 現股名稱查詢（TSE + TPEX）====================

        /// <summary>
        /// 從股票代號查詢股票名稱（涵蓋全市場上市+上櫃）。
        /// 先查 TSE，查無再查 TPEX，皆無回傳 null。
        /// </summary>
        /// <param name="stockCode">股票代號，如 "2330"</param>
        public async Task<string?> GetStockNameAsync(string stockCode)
        {
            try
            {
                await EnsureCacheLoadedAsync();

                if (_tseCache != null && _tseCache.TryGetValue(stockCode, out string? tseName))
                {
                    return tseName;
                }

                if (_tpexCache != null && _tpexCache.TryGetValue(stockCode, out string? tpexName))
                {
                    return tpexName;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ==================== 股票期貨合約查詢（TAIFEX）====================

        /// <summary>
        /// 查詢指定股票代號的期貨合約代號（僅限有股票期貨的標的）。
        /// </summary>
        /// <param name="stockCode">標的股票代號（如 "2330"）</param>
        /// <param name="isSmallContract">是否查詢小型合約</param>
        /// <returns>合約資訊，查無則回傳 null</returns>
        public async Task<ContractInfo?> GetContractInfoAsync(string stockCode, bool isSmallContract)
        {
            try
            {
                await EnsureCacheLoadedAsync();

                if (_taifexCache == null)
                {
                    return null;
                }

                if (_taifexCache.TryGetValue((stockCode, isSmallContract), out ContractInfo? info))
                {
                    return info;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
