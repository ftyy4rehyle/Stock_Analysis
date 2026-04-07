using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public interface IExcelExportService
{
    byte[] ExportStockDetail(StockDetailViewModel model);
    byte[] ExportScreeningResults(List<ScreeningResultViewModel> results, DateOnly date);
}
