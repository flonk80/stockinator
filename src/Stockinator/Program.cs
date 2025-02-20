using Stockinator.Common.DataFetching;
using Stockinator.Common.Extensions;

var client = new HttpClient();

client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

var dataFetcher = new DataFetcher(client);

var result = await dataFetcher.FetchStockPeriodAsync("AAPL", DateTime.Today.AddDays(-10), DateTime.Today);

result.NormalizePrices();

Console.WriteLine("kca");