using System.Text.Json.Serialization;
using Keras.Models;

namespace Stockinator.Common.Models
{
    public class ModelData
    {
        public required string TickerSymbol { get; set; }

        public required string ModelWeightsPath { get; set; }

        public required double[] Loss { get; set; }

        public required double[] ValidationLoss { get; set; }

        public required double[] MeanSquaredError { get; set; }

        public required double[] ValidationMeanSquaredError { get; set; }

        public required double[] TestPredictionValues { get; set; }

        public required double[] TestActualValues { get; set; }

        public required int[] Epochs { get; set; }

        public int TestCount => TestPredictionValues.Length;

        public required long NewestDataTimestamp { get; set; }

        [JsonIgnore]
        public Sequential? SequentialModel { get; set; }
    }
}
