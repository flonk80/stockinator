using Stockinator.Common.Models;
//using Tensorflow.Keras.Engine;
using static Tensorflow.Binding;
//using static Tensorflow.KerasApi;
using Tensorflow.Keras;
using Tensorflow;
//using Tensorflow.Keras.Layers;
//using Tensorflow.Keras.Optimizers;
//using Tensorflow.Keras.ArgsDefinition.Rnn;
using Tensorflow.Keras.ArgsDefinition;
using Python.Runtime;
using Keras;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Numpy;

namespace Stockinator.Logic
{
    public class TensorJoe
    {
        private Dictionary<string, Model> models = new Dictionary<string, Model>();
        private Dictionary<string, NDarray> stockData = new Dictionary<string, NDarray>();
        private List<StockData> stockData2 = new List<StockData>();
        private const int Lookback = 120;  // 120
        private const int FeatureCount = 5; // Open, Close, High, Low, Volume

        public TensorJoe(List<StockData> stockDatas)
        {
            stockData2 = stockDatas;
            
            PythonEngine.Initialize();
            
            //LoadStockData(stockDatas);
        }

        // Loads stock data into memory
        private void LoadStockData(List<StockData> stockDatas)
        {
            foreach (var stock in stockDatas)
            {
                var dataList = stock.DailyStocks
                    .OrderBy(s => s.UnixTimeStamp)
                    .Select(s => new double[] { s.Open, s.Close, s.High, s.Low, s.VolumeNormalized })
                    .ToList(); // Convert to List to manage indexing

                int rows = dataList.Count;
                int cols = dataList[0].Length;

                double[,] dataArray = new double[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        dataArray[i, j] = dataList[i][j]; // Copy values to 2D array
                    }
                }

                stockData.Add(stock.TickerSymbol, np.array(dataArray)); // Now should work
            }
        }

        public float PredictPrice(long timestamp, string tickerSymbol)
        {
            // Ensure model is trained for this ticker
            if (!models.ContainsKey(tickerSymbol))
            {
                Console.WriteLine($"Training model for {tickerSymbol}...");
                TrainModel(tickerSymbol);
            }

            var model = models[tickerSymbol];
            var inputData = PreparePredictionData(tickerSymbol, timestamp);

            if (inputData == null)
            {
                Console.WriteLine("Insufficient data to make a prediction.");
                return float.NaN;
            }

            var prediction = model.Predict(inputData);

            return 0;
        }

        private void TrainModel(string tickerSymbol)
        {
            var (X_train, Y_train) = CreateTrainingData(stockData2[0]);

            var model = BuildLstmModel();
            model.Summary();
            Console.WriteLine(X_train.shape);
            Console.WriteLine(Y_train.shape);

            model.Fit(X_train, Y_train, batch_size: 18, epochs: 25);  // 18, 25

            //models[tickerSymbol] = model;
            //SaveModel(model, tickerSymbol);
        }

        private Sequential BuildLstmModel()
        {
            var model = new Sequential();

            model.Add(new LSTM(units: 64, 
                               input_shape: (Lookback, FeatureCount), 
                               return_sequences: true));

            model.Add(new Dropout(0.1));

            model.Add(new LSTM(units: 64, 
                               return_sequences: false));

            model.Add(new Dense(units: 50, 
                                activation: "relu"));

            model.Add(new Dense(units: 1));

            model.Compile(optimizer: new Adam(lr: 0.001f), loss: "mean_squared_error");

            return model;
        }

        private (NDarray, NDarray) CreateTrainingData(StockData data)
        {
            var dailyStockCount = data.DailyStocks.Count;

            if (dailyStockCount < Lookback)
                throw new ArgumentException("Not enough data points to create training sequences.");

            var xData = new double[dailyStockCount - Lookback + 1, Lookback, FeatureCount];
            var yData = new double[dailyStockCount - Lookback + 1, 1];

            for (int i = 0; i <= dailyStockCount - Lookback; i++)
            {
                for (int j = 0; j < Lookback; j++)
                {
                    xData[i, j, 0] = data.DailyStocks[i + j].CloseNormalized;
                    xData[i, j, 1] = data.DailyStocks[i + j].OpenNormalized;
                    xData[i, j, 2] = data.DailyStocks[i + j].HighNormalized;
                    xData[i, j, 3] = data.DailyStocks[i + j].LowNormalized;
                    xData[i, j, 4] = data.DailyStocks[i + j].VolumeNormalized;
                }
                yData[i, 0] = data.DailyStocks[i + Lookback - 1].CloseNormalized; // Predict the last day's close price
            }

            return (new NDarray(xData), new NDarray(yData));
        }

        private NDarray PreparePredictionData(string tickerSymbol, long timestamp)
        {
            if (!stockData.ContainsKey(tickerSymbol)) return null;

            var data = stockData[tickerSymbol];
            if (data.shape[0] < Lookback) return null;

            return np.expand_dims(data[$"{data.shape[0] - Lookback}:{data.shape[0]}, :"], axis: 0);
        }

        //private void SaveModel(Model model, string tickerSymbol)
        //{
        //    var path = $"{tickerSymbol}_model";
        //    model.save_weights(path);
        //    Console.WriteLine($"Model saved: {path}");
        //}

        //private Model LoadModel(string tickerSymbol)
        //{
        //    var path = $"{tickerSymbol}_model";
        //    if (!File.Exists(path)) return null;

        //    var model = BuildLstmModel(Lookback, FeatureCount);
        //    model.load_weights(path);
        //    Console.WriteLine($"Loaded model: {path}");
        //    return model;
        //}
    }
}
