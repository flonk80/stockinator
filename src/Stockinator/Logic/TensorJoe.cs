using Stockinator.Common.Models;
using Python.Runtime;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Numpy;
using System.Text.Json;

namespace Stockinator.Logic
{
    public class TensorJoe
    {
        private const int BatchSize = 16;
        private const int Epochs = 25;
        private const int Lookback = 30;  // 120
        private const int FeatureCount = 5;
        private const string ModelDataDirectory = "C:\\Stockinator\\ModelData";
        private const string WeightsDirectory = $"{ModelDataDirectory}\\ModelWeights";

        private readonly List<ModelData> _models = [];
        
        public string[] AvailableModels => _models.Select(x => x.TickerSymbol).ToArray();

        public TensorJoe()
        {
            PythonEngine.Initialize();

            Directory.CreateDirectory(WeightsDirectory);

            LoadModels();
        }

        //public float PredictPrice(long timestamp, string tickerSymbol)
        //{
        //    // Ensure model is trained for this ticker
        //    if (!models.ContainsKey(tickerSymbol))
        //    {
        //        Console.WriteLine($"Training model for {tickerSymbol}...");
        //        TrainModel(tickerSymbol);
        //    }

        //    var model = models[tickerSymbol];
        //    var inputData = PreparePredictionData(tickerSymbol, timestamp);

        //    if (inputData == null)
        //    {
        //        Console.WriteLine("Insufficient data to make a prediction.");
        //        return float.NaN;
        //    }

        //    var prediction = model.Predict(inputData);

        //    return 0;
        //}

        public void TrainModel(StockData stockData)
        {
             var (knownData, targetDate) = CreateTrainingData(stockData);

            var model = BuildLstmModel();

            Console.WriteLine("Training model...");
            var modelHistory = model.Fit(knownData, targetDate, batch_size: BatchSize, epochs: Epochs);

            var modelData = new ModelData
            {
                TickerSymbol = stockData.TickerSymbol,
                NewestDataTimestamp = stockData.DailyStocks[^1].UnixTimeStamp,
                ModelWeightsPath = $"{WeightsDirectory}\\{stockData.TickerSymbol}_weights.h5",
                Loss = modelHistory.HistoryLogs["loss"],
                MeanSquaredError = modelHistory.HistoryLogs["mae"],
                SequentialModel = model
            };

            SaveModel(modelData);
        }

        private void LoadModels()
        {
            Console.WriteLine("Loading models...");

            if (_models == null)
            {
                throw new Exception("Model list is null???");
            }

            foreach (var file in Directory.GetFiles(ModelDataDirectory)
                                          .Where(x => x.Contains(".json", StringComparison.OrdinalIgnoreCase)))
            {
                var modelData = JsonSerializer.Deserialize<ModelData>(File.ReadAllText(file)) ?? 
                    throw new Exception($"Error when loading file: {file}");

                var model = BuildLstmModel();
                model.LoadWeight(modelData.ModelWeightsPath);

                modelData.SequentialModel = model;

                _models.Add(modelData);
            }

            Console.WriteLine($"Found and loaded {_models.Count} models!");
        }

        private void SaveModel(ModelData modelData)
        {
            if (modelData.SequentialModel == null)
            {
                throw new Exception($"Can't save a model if the model weights are null: {modelData.TickerSymbol}");
            }

            var modelDataPath = $"{ModelDataDirectory}\\{modelData.TickerSymbol}.json";

            modelData.SequentialModel.SaveWeight(modelData.ModelWeightsPath); // save weights to disk
            File.WriteAllText(modelDataPath, JsonSerializer.Serialize(modelData));

            if (_models.Exists(x => x.TickerSymbol == modelData.TickerSymbol))
            {
                _models[_models.FindIndex(x => x.TickerSymbol == modelData.TickerSymbol)] = modelData;
            }
            else
            {
                _models.Add(modelData);
            }            
        }

        private static Sequential BuildLstmModel()
        {
            var model = new Sequential();

            model.Add(new LSTM(units: 50, // 64
                               input_shape: (Lookback, FeatureCount), 
                               return_sequences: true));

            model.Add(new LSTM(units: 50, // 64
                               return_sequences: false));

            model.Add(new Dense(units: 25, // 32
                                activation: "relu"));

            model.Add(new Dense(units: 1)); // 1

            model.Compile(optimizer: new Adam(lr: 0.001f), loss: "mean_squared_error", metrics: ["mae"]); 

            return model;
        }

        private static (NDarray, NDarray) CreateTrainingData(StockData data)
        {
            var dailyStockCount = data.DailyStocks.Count;

            if (dailyStockCount < Lookback)
            {
                throw new ArgumentException("Not enough data points to create training sequences.");
            }

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
                yData[i, 0] = data.DailyStocks[i + Lookback - 1].CloseNormalized;
            }

            return (new NDarray(xData), new NDarray(yData));
        }

        //private NDarray PreparePredictionData(string tickerSymbol, long timestamp)
        //{
        //    if (!stockData.ContainsKey(tickerSymbol)) return null;

        //    var data = stockData[tickerSymbol];
        //    if (data.shape[0] < Lookback) return null;

        //    return np.expand_dims(data[$"{data.shape[0] - Lookback}:{data.shape[0]}, :"], axis: 0);
        //}

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
