namespace LeverageCalculator.Models
{
    /// <summary>
    /// 股票項目模型
    /// </summary>
    public class StockItem
    {
        /// <summary>
        /// 股票名稱
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 持有股數
        /// </summary>
        public int Shares { get; set; }
        /// <summary>
        /// 帳面損益 (使用者輸入)
        /// </summary>
        public decimal ProfitLoss { get; set; }
        /// <summary>
        /// 報酬率 (使用者輸入, 1.5 = 1.5%)
        /// </summary>
        public double ProfitLossPercentage { get; set; }
        /// <summary>
        /// 總市值
        /// </summary>
        public decimal MarketValue { get; set; }
    }
}
