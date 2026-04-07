using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public interface IStockService
{
    Task<StockDetailViewModel?> GetStockDetailAsync(string symbol, int days = 60);
    Task<List<StockSummaryViewModel>> SearchStocksAsync(string keyword);
    Task<List<string>> GetAllSymbolsAsync();
}
