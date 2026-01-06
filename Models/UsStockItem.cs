namespace LeverageCalculator.Models
{
    /// <summary>
    /// 美股項目模型
    /// </summary>
    public class UsStockItem
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
        /// 總成本
        /// </summary>
        public decimal TotalCost { get; set; }
        /// <summary>
        /// 總市值
        /// </summary>
        public decimal MarketValue { get; set; }
    }
}
