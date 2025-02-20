namespace Stockinator.Common.Models
{
    public class StockData
    {
        public required string TickerSymbol { get; init; }

        public required IEnumerable<DailyStock> DailyStocks { get; init; }
    }
}
