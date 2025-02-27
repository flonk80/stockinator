using Stockinator.Common.Models;
using Python.Runtime;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Numpy;
using System.Text.Json;
using XPlot.Plotly;

namespace Stockinator.Logic
{
    public class TensorJoe
    {
        private const int BatchSize = 16;
        private const int Epochs = 25;
        private const int Lookback = 30;  
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

        public void ShowGraphs(string tickerSymbol)
        {
            var modelData = _models.FirstOrDefault(x => x.TickerSymbol == tickerSymbol) ?? throw new Exception("Model data is null");

            // Loss
            var lossTrace = new Scatter
            {
                x = modelData.Epochs,
                y = modelData.Loss,
                mode = "lines",
                name = "Training Loss"
            };
            var valLossTrace = new Scatter
            {
                x = modelData.Epochs,
                y = modelData.ValidationLoss,
                mode = "lines",
                name = "Validation Training Loss"
            };

            var lossChart = Chart.Plot(new[] { lossTrace, valLossTrace });
            lossChart.WithTitle($"Training & Validation Training Loss for {modelData.TickerSymbol}");
            lossChart.WithYTitle("Loss");
            lossChart.WithXTitle("Epochs");

            // Mean Squared Error
            var maeTrace = new Scatter
            {
                x = modelData.Epochs,
                y = modelData.MeanSquaredError,
                mode = "lines",
                name = "Mean Squared Error"
            };
            var validationMaeTrace = new Scatter
            {
                x = modelData.Epochs,
                y = modelData.ValidationMeanSquaredError,
                mode = "lines",
                name = "Validation Mean Squared Error"
            };

            var maeChart = Chart.Plot(new[] { maeTrace, validationMaeTrace });
            maeChart.WithTitle($"Mean Squared Error & Validation Mean Squared Error for {modelData.TickerSymbol}");
            maeChart.WithYTitle("Mean Squared Error");
            maeChart.WithXTitle("Epochs");

            // Test
            var predictionTrace = new Scatter
            {
                x = modelData.TestCount,
                y = modelData.TestPredictionValues,
                mode = "lines",
                name = "Predicted Value"
            };
            var actualTrace = new Scatter
            {
                x = modelData.TestCount,
                y = modelData.TestActualValues,
                mode = "lines",
                name = "Actual Value"
            };

            var testChart = Chart.Plot(new[] { predictionTrace, actualTrace });
            testChart.WithTitle($"Predicted vs. Actual Values for {modelData.TickerSymbol}");
            testChart.WithYTitle("Stock Price");
            testChart.WithXTitle("Days");

            lossChart.Show();
            maeChart.Show();
            testChart.Show();
        }

        public void TrainModel(StockData stockData)
        {
            var ((xTrain, yTrain), (xValidation, yValidation)) = CreateTrainValTestData(stockData);

            var model = BuildLstmModel();

            Console.WriteLine("Training model...");
            var modelHistory = model.Fit(xTrain, 
                                         yTrain, 
                                         validation_data: [xValidation, yValidation], 
                                         batch_size: BatchSize, 
                                         epochs: Epochs);

            var predictions = model.Predict(xValidation);
            
            var modelData = new ModelData
            {
                TickerSymbol = stockData.TickerSymbol,
                NewestDataTimestamp = stockData.DailyStocks[^1].UnixTimeStamp,
                ModelWeightsPath = $"{WeightsDirectory}\\{stockData.TickerSymbol}_weights.h5",
                Loss = modelHistory.HistoryLogs["loss"],
                ValidationLoss = modelHistory.HistoryLogs["val_loss"],
                MeanSquaredError = modelHistory.HistoryLogs["mae"],
                ValidationMeanSquaredError = modelHistory.HistoryLogs["val_mae"],
                TestPredictionValues = predictions.GetData<float>(),
                TestActualValues = yValidation.GetData<double>(),
                Epochs = modelHistory.Epoch,
                LatestLookback = stockData.DailyStocks.TakeLast(Lookback).ToList(),
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

            model.Add(new LSTM(units: 50, 
                               input_shape: (Lookback, FeatureCount), 
                               return_sequences: true));

            model.Add(new LSTM(units: 50,
                               return_sequences: false));

            model.Add(new Dense(units: 25,
                                activation: "relu"));

            model.Add(new Dense(units: 1));

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

        private static ((NDarray, NDarray), (NDarray, NDarray)) CreateTrainValTestData(StockData data)
        {
            var dailyStockCount = data.DailyStocks.Count;

            if (dailyStockCount < Lookback)
            {
                throw new ArgumentException("Not enough data points to create training sequences.");
            }

            var totalSize = dailyStockCount - Lookback + 1;
            var trainSize = (int)(0.8 * totalSize);

            var xData = new double[totalSize, Lookback, FeatureCount];
            var yData = new double[totalSize, 1];

            for (var i = 0; i < totalSize; i++)
            {
                for (var j = 0; j < Lookback; j++)
                {
                    xData[i, j, 0] = data.DailyStocks[i + j].CloseNormalized;
                    xData[i, j, 1] = data.DailyStocks[i + j].OpenNormalized;
                    xData[i, j, 2] = data.DailyStocks[i + j].HighNormalized;
                    xData[i, j, 3] = data.DailyStocks[i + j].LowNormalized;
                    xData[i, j, 4] = data.DailyStocks[i + j].VolumeNormalized;
                }

                yData[i, 0] = data.DailyStocks[i + Lookback - 1].CloseNormalized;
            }
            
            var xSplit = np.split(new NDarray(xData), [trainSize]);
            var ySplit = np.split(new NDarray(yData), [trainSize]);

            return ((xSplit[0], ySplit[0]), (xSplit[1], ySplit[1]));
        }
    }
}
