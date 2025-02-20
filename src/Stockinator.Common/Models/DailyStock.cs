namespace Stockinator.Common.Models
{
    public class DailyStock
    {
        public required double Open { get; init; }

        public required double Close { get; init; }

        public required double High { get; init; }

        public required double Low { get; init; }

        public required long Volume { get; init; }

        public required long UnixTimeStamp { get; set; }
    }
}
