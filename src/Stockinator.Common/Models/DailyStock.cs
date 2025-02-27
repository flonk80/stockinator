namespace Stockinator.Common.Models
{
    public class DailyStock
    {
        public required long UnixTimeStamp { get; set; }

        public required double Open { get; set; }

        public required double Close { get; set; }

        public required double High { get; set; }

        public required double Low { get; set; }

        public required double Volume { get; set; }

        public double OpenNormalized { get; set; }

        public double CloseNormalized { get; set; }

        public double HighNormalized { get; set; }

        public double LowNormalized { get; set; }

        public double VolumeNormalized { get; set; }
    }
}
