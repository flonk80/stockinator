namespace Stockinator.Common.Models
{
    public class DailyStock
    {
        private static readonly double _normalizationIndex = 10.0;

        private readonly double _globalBaselinePrice;
        private readonly double _globalBaselineVolume;

        public required long UnixTimeStamp { get; set; }

        public required double Open { get; set; }

        public required double Close { get; set; }

        public required double High { get; set; }

        public required double Low { get; set; }

        public required double Volume { get; set; }

        public double OpenNormalized => (Open / _globalBaselinePrice) * _normalizationIndex;

        public double CloseNormalized => (Close / _globalBaselinePrice) * _normalizationIndex;

        public double HighNormalized => (High / _globalBaselinePrice) * _normalizationIndex;

        public double LowNormalized => (Low / _globalBaselinePrice) * _normalizationIndex;

        public double VolumeNormalized => (Volume / _globalBaselineVolume) * _normalizationIndex;

        public DailyStock(double globalBaselinePrice, double globalBaselineVolume)
        {
            _globalBaselinePrice = globalBaselinePrice;
            _globalBaselineVolume = globalBaselineVolume;
        }
    }
}
