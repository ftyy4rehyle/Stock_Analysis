using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public interface IBacktestService
{
    Task<BacktestViewModel> RunAsync(string symbol);
}
