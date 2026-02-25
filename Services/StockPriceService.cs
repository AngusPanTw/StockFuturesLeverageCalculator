using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeverageCalculator.Services
{
    /// <summary>
    /// 股票收盤價查詢結果
    /// </summary>
    public class StockPriceResult
    {
        /// <summary>
        /// 是否成功取得資料
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// 收盤價
        /// </summary>
        public decimal ClosingPrice { get; init; }

        /// <summary>
        /// 股票名稱（中文）
        /// </summary>
        public string StockName { get; init; } = string.Empty;

        /// <summary>
        /// 資料日期（如 "115/02/24"）
        /// </summary>
        public string Date { get; init; } = string.Empty;

        /// <summary>
        /// 資料來源（"TWSE" 或 "TPEX"）
        /// </summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>
        /// 錯誤訊息（失敗時使用）
        /// </summary>
        public string ErrorMessage { get; init; } = string.Empty;
    }

    /// <summary>
    /// 股票收盤價查詢服務，使用 TWSE/TPEX 官方 API 取得盤後收盤價
    /// </summary>
    public class StockPriceService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static StockPriceService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// 查詢股票收盤價（先查上市，失敗再查上櫃）
        /// </summary>
        /// <param name="stockCode">股票代號（如 "2330"）</param>
        /// <returns>查詢結果</returns>
        public async Task<StockPriceResult> GetClosingPriceAsync(string stockCode)
        {
            // 先查 TWSE 上市
            StockPriceResult twseResult = await GetTwseClosingPriceAsync(stockCode);
            if (twseResult.Success)
            {
                return twseResult;
            }

            // 上市查不到，改查 TPEX 上櫃
            StockPriceResult tpexResult = await GetTpexClosingPriceAsync(stockCode);
            if (tpexResult.Success)
            {
                return tpexResult;
            }

            return new StockPriceResult
            {
                Success = false,
                ErrorMessage = $"查無代號 {stockCode} 的股票資料（上市及上櫃皆無結果）"
            };
        }

        /// <summary>
        /// 查詢 TWSE 上市股票收盤價
        /// </summary>
        private async Task<StockPriceResult> GetTwseClosingPriceAsync(string stockCode)
        {
            try
            {
                string today = DateTime.Now.ToString("yyyyMMdd");
                string url = $"https://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date={today}&stockNo={stockCode}";

                string json = await _httpClient.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // 檢查是否有資料
                if (!root.TryGetProperty("data", out JsonElement dataArray)
                    || dataArray.GetArrayLength() == 0)
                {
                    return new StockPriceResult { Success = false, ErrorMessage = "TWSE 無資料" };
                }

                // 取最後一筆（最新日期）的日期（index 0）和收盤價（index 6）
                JsonElement lastRow = dataArray[dataArray.GetArrayLength() - 1];
                string dateStr = lastRow[0].GetString() ?? "";
                string closingPriceStr = lastRow[6].GetString() ?? "";
                closingPriceStr = closingPriceStr.Replace(",", "");

                if (!decimal.TryParse(closingPriceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal closingPrice))
                {
                    return new StockPriceResult { Success = false, ErrorMessage = "TWSE 收盤價格式錯誤" };
                }

                // 從 title 解析股票名稱（格式如 "115年02月 2330 台積電 各日成交資訊"）
                string stockName = string.Empty;
                if (root.TryGetProperty("title", out JsonElement titleElement))
                {
                    string title = titleElement.GetString() ?? string.Empty;
                    stockName = ParseStockNameFromTwseTitle(title, stockCode);
                }

                return new StockPriceResult
                {
                    Success = true,
                    ClosingPrice = closingPrice,
                    StockName = stockName,
                    Date = dateStr,
                    Source = "TWSE"
                };
            }
            catch (Exception ex)
            {
                return new StockPriceResult { Success = false, ErrorMessage = $"TWSE 查詢失敗: {ex.Message}" };
            }
        }

        /// <summary>
        /// 查詢 TPEX 上櫃股票收盤價
        /// </summary>
        private async Task<StockPriceResult> GetTpexClosingPriceAsync(string stockCode)
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy/MM/dd");
                string url = $"https://www.tpex.org.tw/www/zh-tw/afterTrading/tradingStock?response=json&date={today}&code={stockCode}";

                string json = await _httpClient.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // 檢查 tables[0].data 是否有資料
                if (!root.TryGetProperty("tables", out JsonElement tablesArray)
                    || tablesArray.GetArrayLength() == 0)
                {
                    return new StockPriceResult { Success = false, ErrorMessage = "TPEX 無資料" };
                }

                JsonElement firstTable = tablesArray[0];
                if (!firstTable.TryGetProperty("data", out JsonElement dataArray)
                    || dataArray.GetArrayLength() == 0)
                {
                    return new StockPriceResult { Success = false, ErrorMessage = "TPEX 無資料" };
                }

                // 取最後一筆的日期（index 0）和收盤價（index 6）
                JsonElement lastRow = dataArray[dataArray.GetArrayLength() - 1];
                string dateStr = lastRow[0].GetString() ?? "";
                string closingPriceStr = lastRow[6].GetString() ?? "";
                closingPriceStr = closingPriceStr.Replace(",", "");

                if (!decimal.TryParse(closingPriceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal closingPrice))
                {
                    return new StockPriceResult { Success = false, ErrorMessage = "TPEX 收盤價格式錯誤" };
                }

                // 從頂層 name 欄位取得股票名稱
                string stockName = string.Empty;
                if (root.TryGetProperty("name", out JsonElement nameElement))
                {
                    stockName = nameElement.GetString() ?? string.Empty;
                }

                return new StockPriceResult
                {
                    Success = true,
                    ClosingPrice = closingPrice,
                    StockName = stockName,
                    Date = dateStr,
                    Source = "TPEX"
                };
            }
            catch (Exception ex)
            {
                return new StockPriceResult { Success = false, ErrorMessage = $"TPEX 查詢失敗: {ex.Message}" };
            }
        }

        /// <summary>
        /// 從 TWSE title 解析股票名稱
        /// 格式: "115年02月 2330 台積電 各日成交資訊"
        /// </summary>
        private static string ParseStockNameFromTwseTitle(string title, string stockCode)
        {
            // 找到代號後的文字，到 "各日成交資訊" 之前
            int codeIndex = title.IndexOf(stockCode, StringComparison.Ordinal);
            if (codeIndex < 0)
            {
                return string.Empty;
            }

            int nameStart = codeIndex + stockCode.Length;
            int nameEnd = title.IndexOf("各日成交資訊", StringComparison.Ordinal);
            if (nameEnd < 0)
            {
                nameEnd = title.Length;
            }

            string name = title[nameStart..nameEnd].Trim();
            return name;
        }
    }
}
