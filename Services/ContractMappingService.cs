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
    /// 合約代號對照服務，使用 TAIFEX SingleStockFuturesMargining API
    /// 從股票代號查出對應的期貨合約代號
    /// </summary>
    public class ContractMappingService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://openapi.taifex.com.tw/v1/SingleStockFuturesMargining";

        /// <summary>
        /// 記憶體內快取，程式啟動後第一次查詢時載入，之後不再重新請求 API
        /// </summary>
        private List<JsonElement>? _cachedItems;

        static ContractMappingService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 取得快取的 API 資料，若尚未載入則從 API 抓取
        /// </summary>
        private async Task<List<JsonElement>> GetCachedItemsAsync()
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

        /// <summary>
        /// 查詢指定股票代號的期貨合約代號
        /// </summary>
        /// <param name="stockCode">標的股票代號（如 "2330"）</param>
        /// <param name="isSmallContract">是否查詢小型合約</param>
        /// <returns>合約資訊，查無則回傳 null</returns>
        public async Task<ContractInfo?> GetContractInfoAsync(string stockCode, bool isSmallContract)
        {
            try
            {
                List<JsonElement> items = await GetCachedItemsAsync();

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

                    if (!item.TryGetProperty("ContractName", out JsonElement nameEl)
                        || !item.TryGetProperty("Contract", out JsonElement contractEl))
                    {
                        continue;
                    }

                    string contractName = nameEl.GetString() ?? string.Empty;
                    string contractCode = contractEl.GetString() ?? string.Empty;

                    bool isSmall = contractName.Contains("小型");

                    if (isSmall == isSmallContract)
                    {
                        return new ContractInfo
                        {
                            ContractCode = contractCode,
                            ContractName = contractName
                        };
                    }
                }

                return null;
            }
            catch
            {
                _cachedItems = null;
                return null;
            }
        }
    }
}
