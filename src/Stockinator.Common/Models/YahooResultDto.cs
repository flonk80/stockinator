namespace Stockinator.Common.Models
{
    public class YahooResultDto
    {
        public List<long>? TimeStamp { get; set; }

        public YahooIndicatorsDto? Indicators { get; set; }
    }
}
