using System.IO;
using System.Text.Json;
using LeverageCalculator.Models;

namespace LeverageCalculator.Services
{
    /// <summary>
    /// 投資組合儲存服務，負責將 Portfolio 資料序列化並儲存至 JSON 檔案
    /// </summary>
    public class PortfolioStorageService
    {
        private readonly string _filePath;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="fileName">儲存檔案名稱</param>
        public PortfolioStorageService(string fileName)
        {
            _filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        }

        /// <summary>
        /// 載入投資組合資料
        /// </summary>
        /// <returns>載入的投資組合物件，若檔案不存在或讀取失敗則回傳 null</returns>
        public Portfolio? LoadPortfolio()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Portfolio>(json);
            }
            catch
            {
                // Handle potential errors like corrupted file
                return null;
            }
        }

        /// <summary>
        /// 儲存投資組合資料
        /// </summary>
        /// <param name="portfolio">要儲存的投資組合物件</param>
        public void SavePortfolio(Portfolio portfolio)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(portfolio, options);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Handle potential saving errors
            }
        }
    }
}
