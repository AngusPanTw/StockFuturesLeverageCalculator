namespace LeverageCalculator.Models
{
    /// <summary>
    /// 期貨項目模型
    /// </summary>
    public class FutureItem
    {
        /// <summary>
        /// 標的名稱
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
        /// 成本價格
        /// </summary>
        public decimal CostPrice { get; set; }
        /// <summary>
        /// 目前市價
        /// </summary>
        public decimal CurrentPrice { get; set; }
        /// <summary>
        /// 是否為小型合約 (true: 100股, false: 2000股)
        /// </summary>
        public bool IsSmallContract { get; set; }
    }
}
