﻿using Stockinator.Common.Models;
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
                var quote = chart.Result[0].Indicators?.Quote?[0] ?? throw new Exception("Quote is null");

                var stockData = new StockData
                {
                    TickerSymbol = tickerSymbol,
                    DailyStocks = timeStamps.Select((x, i) => new DailyStock
                    {
                        UnixTimeStamp = x,
                        Close = quote.Close?[i] ?? throw new Exception("Close is invalid"),
                        High = quote.High?[i] ?? throw new Exception("High is invalid"),
                        Low = quote.Low?[i] ?? throw new Exception("Low is invalid"),
                        Open = quote.Open?[i] ?? throw new Exception("Open is invalid"),
                        Volume = quote.Volume?[i] ?? throw new Exception("Volume is invalid"),
                    }).ToList()
                };

                foreach (var dailyStock in stockData.DailyStocks)
                {
                    dailyStock.OpenNormalized = (dailyStock.Open - quote.Open.Min()) / (quote.Open.Max() - quote.Open.Min());
                    dailyStock.CloseNormalized = (dailyStock.Close - quote.Close.Min()) / (quote.Close.Max() - quote.Close.Min());
                    dailyStock.HighNormalized = (dailyStock.High - quote.High.Min()) / (quote.High.Max() - quote.High.Min());
                    dailyStock.LowNormalized = (dailyStock.Low - quote.Low.Min()) / (quote.Low.Max() - quote.Low.Min());
                    dailyStock.VolumeNormalized = (dailyStock.Volume - quote.Volume.Min()) / (quote.Volume.Max() - quote.Volume.Min());
                }

                return stockData;
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
