using System.Collections.Generic;

namespace LeverageCalculator.Models
{
    /// <summary>
    /// 投資組合資料模型 (用於儲存與載入)
    /// </summary>
    public class Portfolio
    {
        /// <summary>
        /// 股票列表
        /// </summary>
        public List<StockItem> Stocks { get; set; } = new List<StockItem>();
        /// <summary>
        /// 期貨列表
        /// </summary>
        public List<FutureItem> Futures { get; set; } = new List<FutureItem>();
        /// <summary>
        /// 銀行可用現金
        /// </summary>
        public decimal BankCash { get; set; }
        /// <summary>
        /// 股票交割款 (近三日)
        /// </summary>
        public decimal StockSettlementAmount { get; set; }
        /// <summary>
        /// 期貨帳戶權益金
        /// </summary>
        public decimal FuturesEquity { get; set; }
    }
}
