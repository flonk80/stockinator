using Stockinator.Common.DataFetching;
using Stockinator.Logic;

var client = new HttpClient();

const string Ticker = "MSFT";

client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

var dataFetcher = new DataFetcher(client);

var result = await dataFetcher.FetchStockPeriodAsync(Ticker, DateTime.Today.AddDays(-183), DateTime.Today.AddDays(-1));

var joe = new TensorJoe();

joe.TrainModel(result);

joe.ShowGraphs(Ticker);

Console.ReadLine();