using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace LeverageCalculator.Services
{
    /// <summary>
    /// 單筆期貨收盤價查詢結果
    /// </summary>
    public class FuturesPriceResult
    {
        /// <summary>
        /// 最後成交價（收盤價）
        /// </summary>
        public decimal ClosingPrice { get; init; }

        /// <summary>
        /// 交易日期（格式如 "20260224"）
        /// </summary>
        public string Date { get; init; } = string.Empty;
    }

    /// <summary>
    /// 批次查詢結果
    /// </summary>
    public class FuturesBatchResult
    {
        /// <summary>
        /// 是否成功取得資料
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// 以 Contract+ContractMonth 為 key 的收盤價字典（如 "DIF202603"）
        /// </summary>
        public Dictionary<string, FuturesPriceResult> Prices { get; init; } = new();

        /// <summary>
        /// 錯誤訊息（失敗時使用）
        /// </summary>
        public string ErrorMessage { get; init; } = string.Empty;
    }

    /// <summary>
    /// 股票期貨收盤價查詢服務，使用 TAIFEX OpenAPI 取得盤後行情
    /// </summary>
    public class FuturesPriceService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://openapi.taifex.com.tw/v1/DailyMarketReportFut";

        static FuturesPriceService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 一次取得所有期貨的每日行情，過濾後建立 key-value 對照表
        /// </summary>
        public async Task<FuturesBatchResult> GetAllFuturesPricesAsync()
        {
            try
            {
                string json = await _httpClient.GetStringAsync(ApiUrl);
                List<JsonElement> items = JsonSerializer.Deserialize<List<JsonElement>>(json)
                    ?? new List<JsonElement>();

                Dictionary<string, FuturesPriceResult> prices = new Dictionary<string, FuturesPriceResult>();

                foreach (JsonElement item in items)
                {
                    // 使用 TryGetProperty 避免單筆缺欄位時拋 exception 導致整批失敗
                    if (!item.TryGetProperty("TradingSession", out JsonElement sessionEl))
                    {
                        continue;
                    }
                    string tradingSession = sessionEl.GetString() ?? string.Empty;
                    if (tradingSession != "一般")
                    {
                        continue;
                    }

                    if (!item.TryGetProperty("ContractMonth(Week)", out JsonElement monthEl))
                    {
                        continue;
                    }
                    string contractMonth = monthEl.GetString() ?? string.Empty;
                    if (contractMonth.Contains('/'))
                    {
                        continue;
                    }

                    if (!item.TryGetProperty("Last", out JsonElement lastEl))
                    {
                        continue;
                    }
                    string lastStr = (lastEl.GetString() ?? string.Empty).Replace(",", string.Empty);
                    if (string.IsNullOrEmpty(lastStr) || lastStr == "-")
                    {
                        continue;
                    }

                    if (!decimal.TryParse(lastStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal closingPrice))
                    {
                        continue;
                    }

                    if (!item.TryGetProperty("Contract", out JsonElement contractEl)
                        || !item.TryGetProperty("Date", out JsonElement dateEl))
                    {
                        continue;
                    }

                    string contract = contractEl.GetString() ?? string.Empty;
                    string date = dateEl.GetString() ?? string.Empty;
                    string key = contract + contractMonth;

                    prices[key] = new FuturesPriceResult
                    {
                        ClosingPrice = closingPrice,
                        Date = date
                    };
                }

                return new FuturesBatchResult
                {
                    Success = true,
                    Prices = prices
                };
            }
            catch (Exception ex)
            {
                return new FuturesBatchResult
                {
                    Success = false,
                    ErrorMessage = $"TAIFEX 查詢失敗: {ex.Message}"
                };
            }
        }
    }
}
