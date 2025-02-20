namespace Stockinator.Common.Models
{
    public class YahooChartDto
    {
        public List<YahooResultDto>? Result { get; set; }

        public YahooErrorDto? Error { get; set; }
    }
}
