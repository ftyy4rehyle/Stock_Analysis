using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public interface IScreeningService
{
    Task<List<ScreeningResultViewModel>> GetScreeningResultsAsync(DateOnly? date = null);
    Task<List<DateOnly>> GetAvailableDatesAsync(int count = 30);
    Task RunDailyScreeningAsync(DateOnly date);
}
