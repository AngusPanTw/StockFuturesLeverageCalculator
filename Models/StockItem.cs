namespace LeverageCalculator.Models
{
    /// <summary>
    /// 股票項目模型
    /// </summary>
    public class StockItem
    {
        /// <summary>
        /// 股票代號（如 "2330"）
        /// </summary>
        public string StockCode { get; set; } = string.Empty;
        /// <summary>
        /// 股票名稱
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 持有股數
        /// </summary>
        public int Shares { get; set; }
        /// <summary>
        /// 進場均價
        /// </summary>
        public decimal EntryPrice { get; set; }
        /// <summary>
        /// 現價
        /// </summary>
        public decimal CurrentPrice { get; set; }
        /// <summary>
        /// 持有類型（現股/融資）
        /// </summary>
        public StockType StockType { get; set; } = StockType.Cash;
        /// <summary>
        /// 融資成數（券商借出比例，預設 0.6 = 融資六成）
        /// </summary>
        public decimal MarginRatio { get; set; } = 0.6m;
    }
}
