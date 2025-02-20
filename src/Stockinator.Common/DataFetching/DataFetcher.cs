using Stockinator.Common.Models;
using System.Net.Http.Json;

namespace Stockinator.Common.DataFetching
{
    public class DataFetcher
    {
        private const string StockInterval = "1d";
        private const string TickerSymbolPlaceholder = "TICKER";
        private const string FromPlaceholder = "FROM";
        private const string ToPlaceholder = "TO";
        private const string UrlTemplate = $"https://query1.finance.yahoo.com/v8/finance/chart/{TickerSymbolPlaceholder}?period1={FromPlaceholder}&period2={ToPlaceholder}&interval={StockInterval}";

        private readonly HttpClient _httpClient;

        public DataFetcher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<StockData> FetchStockPeriodAsync(string tickerSymbol, DateTime from, DateTime to)
        {
            var fromUnix = GetUnixTimestamp(from);
            var toUnix = GetUnixTimestamp(to);
            var url = BuildUrl(tickerSymbol, fromUnix, toUnix);

            var httpResponse = await _httpClient.GetAsync(url);
            var str = await httpResponse.Content.ReadAsStringAsync();
            var response = await httpResponse.Content.ReadFromJsonAsync<YahooRootDto>();
            var chart = response?.Chart ?? throw new Exception("Response is null");
            
            if (chart is not null)
            {
                if (chart.Error != null)
                {
                    throw new Exception(chart.Error.Description);
                }

                var timeStamps = chart.Result?[0].TimeStamp ?? throw new Exception("Timestamp is null");
                var quotes = chart.Result[0].Indicators?.Quote ?? throw new Exception("Quotes is null");

                return new StockData
                {
                    TickerSymbol = tickerSymbol,
                    DailyStocks = timeStamps.Select((x, i) => new DailyStock
                    {
                        UnixTimeStamp = timeStamps[i],
                        Close = quotes[0]?.Close?[i] ?? throw new Exception("Close is invalid"),
                        High = quotes[0]?.High?[i] ?? throw new Exception("High is invalid"),
                        Low = quotes[0]?.Low?[i] ?? throw new Exception("Low is invalid"),
                        Open = quotes[0]?.Open?[i] ?? throw new Exception("Open is invalid"),
                        Volume = quotes[0]?.Volume?[i] ?? throw new Exception("Volume is invalid"),
                    })
                };
            }

            throw new Exception("Couldn't fetch stock period");
        }

        private static string BuildUrl(string tickerSymbol, long fromUnix, long toUnix) => 
            UrlTemplate.Replace(TickerSymbolPlaceholder, tickerSymbol)
                       .Replace(FromPlaceholder, fromUnix.ToString())
                       .Replace(ToPlaceholder, toUnix.ToString());

        private static long GetUnixTimestamp(DateTime time) => 
            ((DateTimeOffset)time).ToUnixTimeSeconds();
    }
}
