namespace Stockinator.Common.Models
{
    public class StockData
    {
        public required string TickerSymbol { get; set; }

        public required List<DailyStock> DailyStocks { get; set; }
    }
}
