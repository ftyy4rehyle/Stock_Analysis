using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public class StockService : IStockService
{
    private readonly StockDbContext _db;

    public StockService(StockDbContext db)
    {
        _db = db;
    }

    public async Task<StockDetailViewModel?> GetStockDetailAsync(string symbol, int days = 60)
    {
        var stock = await _db.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Symbol == symbol && s.IsActive);

        if (stock == null) return null;

        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-(days + 30)));

        var quotes = await _db.DailyQuotes
            .AsNoTracking()
            .Where(q => q.StockId == stock.Id && q.TradeDate >= cutoff)
            .OrderBy(q => q.TradeDate)
            .ToListAsync();

        var indicators = await _db.TechnicalIndicators
            .AsNoTracking()
            .Where(t => t.StockId == stock.Id && t.TradeDate >= cutoff)
            .ToDictionaryAsync(t => t.TradeDate);

        var recentQuotes = quotes.TakeLast(days).ToList();

        var chartData = recentQuotes.Select(q =>
        {
            indicators.TryGetValue(q.TradeDate, out var ind);
            return new DailyChartPoint
            {
                Date          = q.TradeDate.ToString("yyyy/MM/dd"),
                Close         = q.Close,
                MA5           = ind?.MA5,
                MA20          = ind?.MA20,
                MA60          = ind?.MA60,
                Volume        = q.Volume,
                Change        = q.Change,
                ChangePercent = q.ChangePercent,
                IsBuySignal   = ind?.IsBuySignal  ?? false,
                IsSellSignal  = ind?.IsSellSignal ?? false
            };
        }).ToList();

        var latest = recentQuotes.LastOrDefault();
        indicators.TryGetValue(latest?.TradeDate ?? DateOnly.MinValue, out var latestInd);

        return new StockDetailViewModel
        {
            Symbol = stock.Symbol,
            Name = stock.Name,
            Industry = stock.Industry,
            Market = stock.Market,
            ChartData = chartData,
            LatestClose = latest?.Close,
            LatestChange = latest?.Change,
            LatestChangePercent = latest?.ChangePercent,
            LatestVolume = latest?.Volume,
            MA5 = latestInd?.MA5,
            MA20 = latestInd?.MA20,
            MA60 = latestInd?.MA60
        };
    }

    public async Task<List<StockSummaryViewModel>> SearchStocksAsync(string keyword)
    {
        keyword = keyword.Trim();
        return await _db.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && (s.Symbol.Contains(keyword) || s.Name.Contains(keyword)))
            .OrderBy(s => s.Symbol)
            .Take(20)
            .Select(s => new StockSummaryViewModel
            {
                Symbol = s.Symbol,
                Name = s.Name,
                Industry = s.Industry,
                Market = s.Market
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetAllSymbolsAsync()
    {
        return await _db.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Symbol)
            .Select(s => s.Symbol)
            .ToListAsync();
    }
}
