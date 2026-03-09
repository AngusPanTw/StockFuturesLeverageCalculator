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
    /// TAIFEX 標的資料查詢服務，使用 SingleStockFuturesMargining API
    /// 提供股票名稱查詢（共用同一份快取資料）
    /// </summary>
    public class ContractMappingService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://openapi.taifex.com.tw/v1/SingleStockFuturesMargining";

        /// <summary>
        /// 記憶體內快取，程式啟動後第一次查詢時載入，之後不再重新請求 API
        /// </summary>
        private List<JsonElement>? _cachedItems;

        /// <summary>
        /// 防止並行初始化快取的鎖
        /// </summary>
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        static ContractMappingService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 取得快取的 API 資料，若尚未載入則從 API 抓取（執行緒安全）
        /// </summary>
        private async Task<List<JsonElement>> GetCachedItemsAsync()
        {
            if (_cachedItems != null)
            {
                return _cachedItems;
            }

            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedItems != null)
                {
                    return _cachedItems;
                }

                string json = await _httpClient.GetStringAsync(ApiUrl);
                _cachedItems = JsonSerializer.Deserialize<List<JsonElement>>(json)
                    ?? new List<JsonElement>();
                return _cachedItems;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// 預熱快取，啟動時 fire-and-forget 呼叫以避免首次查詢延遲。
        /// 內部已有 try-catch，不會拋出例外。
        /// </summary>
        public async Task PreloadCacheAsync()
        {
            try
            {
                await GetCachedItemsAsync();
            }
            catch
            {
                // 預熱失敗不影響後續功能，後續查詢會再次嘗試載入
            }
        }

        /// <summary>
        /// 從股票代號查詢股票名稱（如 "2330" → "台積電"）
        /// 利用 ContractName 去掉「小型」前綴和「期貨」後綴，優先取大型合約名稱
        /// </summary>
        public async Task<string?> GetStockNameAsync(string stockCode)
        {
            try
            {
                List<JsonElement> items = await GetCachedItemsAsync();
                string? fallbackName = null;

                foreach (JsonElement item in items)
                {
                    if (!item.TryGetProperty("UnderlyingSecurityCode", out JsonElement codeEl))
                    {
                        continue;
                    }

                    string underlyingCode = codeEl.GetString() ?? string.Empty;
                    if (underlyingCode != stockCode)
                    {
                        continue;
                    }

                    if (!item.TryGetProperty("ContractName", out JsonElement nameEl))
                    {
                        continue;
                    }

                    string contractName = nameEl.GetString() ?? string.Empty;
                    string stockName = contractName
                        .Replace("小型", string.Empty)
                        .Replace("期貨", string.Empty)
                        .Trim();

                    if (string.IsNullOrEmpty(stockName))
                    {
                        continue;
                    }

                    // 優先取大型合約名稱，立即回傳
                    if (!contractName.Contains("小型"))
                    {
                        return stockName;
                    }

                    fallbackName ??= stockName;
                }

                return fallbackName;
            }
            catch
            {
                return null;
            }
        }
    }
}
