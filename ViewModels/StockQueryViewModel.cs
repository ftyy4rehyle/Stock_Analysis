namespace StockAnalysis.ViewModels;

public class StockQueryViewModel
{
    public string? Symbol { get; set; }
    public int Days { get; set; } = 60;
    public StockDetailViewModel? Detail { get; set; }
    public string? ErrorMessage { get; set; }
}
