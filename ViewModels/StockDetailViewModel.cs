namespace StockAnalysis.ViewModels;

public class StockDetailViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Market { get; set; }

    public List<DailyChartPoint> ChartData { get; set; } = new();
    public decimal? LatestClose { get; set; }
    public decimal? LatestChange { get; set; }
    public decimal? LatestChangePercent { get; set; }
    public long? LatestVolume { get; set; }
    public decimal? MA5 { get; set; }
    public decimal? MA20 { get; set; }
    public decimal? MA60 { get; set; }
}

public class DailyChartPoint
{
    public string Date { get; set; } = string.Empty;
    public decimal Close { get; set; }
    public decimal? MA5 { get; set; }
    public decimal? MA20 { get; set; }
    public decimal? MA60 { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}
