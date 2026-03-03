namespace LeverageCalculator.Models
{
    /// <summary>
    /// 投資組合資料模型 (用於儲存與載入)
    /// </summary>
    public class Portfolio
    {
        /// <summary>
        /// 期貨列表
        /// </summary>
        public List<FutureItem> Futures { get; set; } = new List<FutureItem>();

        /// <summary>
        /// 群組名稱列表（保持群組順序）
        /// </summary>
        public List<string> GroupNames { get; set; } = new List<string>();
    }
}
