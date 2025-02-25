using Stockinator.Common.Models;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;
using Tensorflow.Keras;
using Tensorflow;
using Tensorflow.Keras.Layers;
using Tensorflow.Keras.Optimizers;
using Tensorflow.Keras.ArgsDefinition.Rnn;
using Tensorflow.Keras.ArgsDefinition;

namespace Stockinator.Logic
{
    public class TensorJoe
    {
        private Dictionary<string, Model> models = new Dictionary<string, Model>();
        private Dictionary<string, NDArray> stockData = new Dictionary<string, NDArray>();
        private const int Lookback = 30;  // Days to look back for prediction
        private const int FeatureCount = 5; // Open, Close, High, Low, Volume

        public TensorJoe(List<StockData> stockDatas)
        {
            LoadStockData(stockDatas);
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

            var prediction = model.predict(inputData);

            return prediction.First().numpy()[0];
        }

        private void TrainModel(string tickerSymbol)
        {
            if (!stockData.ContainsKey(tickerSymbol))
            {
                throw new Exception($"No data available for {tickerSymbol}");
            }

            var data = stockData[tickerSymbol];
            var (X_train, Y_train) = CreateTrainingData(data);

            var model = BuildLstmModel(Lookback, FeatureCount);

            model.fit(X_train, Y_train, batch_size: 16, epochs: 10);

            models[tickerSymbol] = model;
            SaveModel(model, tickerSymbol);
        }

        private Model BuildLstmModel(int lookback, int featureCount)
        {
            var model = keras.Sequential();

            model.Layers.AddRange(new List<ILayer>
            {
                new LSTM(new LSTMArgs
                {
                    Units = 50,
                    ReturnSequences = true,
                    InputShape = new[] { lookback, featureCount}
                }),
                new LSTM(new LSTMArgs 
                { 
                    Units = 50, 
                    ReturnSequences = false 
                }),
                new Dense(new DenseArgs
                {
                    Units = 25,
                    Activation = keras.activations.Relu
                }),
                new Dense(new DenseArgs 
                { 
                    Units = 1 
                })
            });

            model.compile(optimizer: keras.optimizers.Adam(), loss: keras.losses.MeanSquaredError());

            return model;
        }

        private (NDArray, NDArray) CreateTrainingData(NDArray data)
        {
            var X = new List<NDArray>();
            var Y = new List<NDArray>();

            for (int i = Lookback; i < data.shape[0]; i++)
            {
                // Correct way to slice in NumSharp
                var xSlice = data[$"{i - Lookback}:{i}, :"].reshape(new Shape(Lookback, -1));
                var ySlice = data[i, 1].reshape(new Shape(1, 1)); // Extract single target value

                X.Add(xSlice);
                Y.Add(ySlice);
            }

            return (np.concatenate([.. X]), np.concatenate([.. Y]));
        }

        private NDArray PreparePredictionData(string tickerSymbol, long timestamp)
        {
            if (!stockData.ContainsKey(tickerSymbol)) return null;

            var data = stockData[tickerSymbol];
            if (data.shape[0] < Lookback) return null;

            return np.expand_dims(data[$"{data.shape[0] - Lookback}:{data.shape[0]}, :"], axis: 0);
        }

        private void SaveModel(Model model, string tickerSymbol)
        {
            var path = $"{tickerSymbol}_model";
            model.save_weights(path);
            Console.WriteLine($"Model saved: {path}");
        }

        private Model LoadModel(string tickerSymbol)
        {
            var path = $"{tickerSymbol}_model";
            if (!File.Exists(path)) return null;

            var model = BuildLstmModel(Lookback, FeatureCount);
            model.load_weights(path);
            Console.WriteLine($"Loaded model: {path}");
            return model;
        }
    }
}
