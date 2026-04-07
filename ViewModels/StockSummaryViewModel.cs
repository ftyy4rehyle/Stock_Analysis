namespace StockAnalysis.ViewModels;

public class StockSummaryViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Market { get; set; }
}
