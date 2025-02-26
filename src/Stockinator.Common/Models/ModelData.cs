using System.Text.Json.Serialization;
using Keras.Models;

namespace Stockinator.Common.Models
{
    public class ModelData
    {
        public required string TickerSymbol { get; set; }

        public required string ModelWeightsPath { get; set; }

        public required double[] Loss { get; set; }

        public required double[] MeanSquaredError { get; set; }

        public required long NewestDataTimestamp { get; set; }

        [JsonIgnore]
        public Sequential? SequentialModel { get; set; }
    }
}
