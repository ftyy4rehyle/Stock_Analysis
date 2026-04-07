namespace StockAnalysis.ViewModels;

public class ScreeningResultViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public DateOnly ScreenDate { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal AvgVolume20 { get; set; }
    public decimal VolumeRatio { get; set; }
    public decimal MA20 { get; set; }
    public decimal ChangePercent { get; set; }
}

public class ScreeningPageViewModel
{
    public DateOnly? SelectedDate { get; set; }
    public List<DateOnly> AvailableDates { get; set; } = new();
    public List<ScreeningResultViewModel> Results { get; set; } = new();
}
