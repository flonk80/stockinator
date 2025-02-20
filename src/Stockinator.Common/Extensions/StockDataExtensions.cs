using Stockinator.Common.Models;

namespace Stockinator.Common.Extensions
{
    public static class StockDataExtensions
    {
        public static void NormalizePrices(this StockData stockData)
        {
            stockData.DailyStocks = stockData.DailyStocks.Select((x, i) =>
            {
                if (i == 0)
                {
                    x.LowNormalized = 1000;
                    x.HighNormalized = 1000;
                    x.OpenNormalized = 1000;
                    x.CloseNormalized = 1000;

                    return x;
                }

                x.LowNormalized = (x.Low / stockData.DailyStocks.First().Low) * 1000;
                x.HighNormalized = (x.High / stockData.DailyStocks.First().High) * 1000;
                x.OpenNormalized = (x.Open / stockData.DailyStocks.First().Open) * 1000;
                x.CloseNormalized = (x.Close / stockData.DailyStocks.First().Close) * 1000;

                return x;
            });
        }
    }
}
