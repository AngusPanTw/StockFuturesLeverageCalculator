namespace LeverageCalculator.Models
{
    /// <summary>
    /// 期貨項目模型
    /// </summary>
    public class FutureItem
    {
        /// <summary>
        /// 標的股票代號（如 "2330"），用於 API 查詢合約代號
        /// </summary>
        public string UnderlyingStockCode { get; set; } = string.Empty;

        /// <summary>
        /// 合約月份（如 "2603"），用於組合完整期貨代號
        /// </summary>
        public string ContractMonth { get; set; } = string.Empty;

        /// <summary>
        /// 完整期貨代碼（自動組合，如 "CDF202603"）
        /// </summary>
        public string StockCode { get; set; } = string.Empty;

        /// <summary>
        /// 合約名稱（自動從 API 填入，如「台積電期貨」）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 持倉口數
        /// </summary>
        public int Lots { get; set; }

        /// <summary>
        /// 多空方向
        /// </summary>
        public PositionType Position { get; set; } = PositionType.Long;

        /// <summary>
        /// 目前市價（收盤價查詢更新）
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// 是否為小型合約 (true: 100股, false: 2000股)
        /// </summary>
        public bool IsSmallContract { get; set; }

        /// <summary>
        /// 所屬群組名稱（空字串表示未分群）
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
    }
}
