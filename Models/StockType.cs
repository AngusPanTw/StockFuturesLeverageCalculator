namespace LeverageCalculator.Models
{
    /// <summary>
    /// 股票持有類型
    /// </summary>
    public enum StockType
    {
        /// <summary>
        /// 現股（全額交割）
        /// </summary>
        Cash,
        /// <summary>
        /// 融資
        /// </summary>
        Margin
    }
}
