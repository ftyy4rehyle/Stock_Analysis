using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.Models;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public class ScreeningService : IScreeningService
{
    private readonly StockDbContext _db;

    public ScreeningService(StockDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScreeningResultViewModel>> GetScreeningResultsAsync(DateOnly? date = null)
    {
        var targetDate = date ?? await GetLatestScreenDateAsync();
        if (targetDate == default) return new List<ScreeningResultViewModel>();

        return await _db.DailyScreenResults
            .AsNoTracking()
            .Include(r => r.Stock)
            .Where(r => r.ScreenDate == targetDate)
            .Select(r => new ScreeningResultViewModel
            {
                Symbol        = r.Stock.Symbol,
                Name          = r.Stock.Name,
                Industry      = r.Stock.Industry,
                ScreenDate    = r.ScreenDate,
                Close         = r.Close,
                Volume        = r.Volume,
                AvgVolume20   = r.AvgVolume20,
                VolumeRatio   = r.VolumeRatio,
                MA20          = r.MA20,
                ChangePercent = r.ChangePercent
            })
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderByDescending(r => r.VolumeRatio).ToList());
    }

    public async Task<List<DateOnly>> GetAvailableDatesAsync(int count = 30)
    {
        return await _db.DailyScreenResults
            .AsNoTracking()
            .Select(r => r.ScreenDate)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(count)
            .ToListAsync();
    }

    public async Task RunDailyScreeningAsync(DateOnly date)
    {
        // 移除當天舊結果
        var existing = await _db.DailyScreenResults
            .Where(r => r.ScreenDate == date)
            .ToListAsync();
        _db.DailyScreenResults.RemoveRange(existing);

        var stocks = await _db.Stocks.Where(s => s.IsActive).ToListAsync();
        var newResults = new List<DailyScreenResult>();

        foreach (var stock in stocks)
        {
            var quotes = await _db.DailyQuotes
                .Where(q => q.StockId == stock.Id && q.TradeDate <= date)
                .OrderByDescending(q => q.TradeDate)
                .Take(21)
                .ToListAsync();

            if (quotes.Count < 20) continue;

            var todayQuote = quotes.FirstOrDefault(q => q.TradeDate == date);
            if (todayQuote == null) continue;

            var last20 = quotes.OrderBy(q => q.TradeDate).ToList();
            var ma20 = last20.TakeLast(20).Average(q => q.Close);
            var avgVol20 = (decimal)last20.TakeLast(20).Average(q => q.Volume);

            if (todayQuote.Volume > avgVol20 * 1.5m && todayQuote.Close > ma20)
            {
                var prevQuote = quotes.Skip(1).FirstOrDefault();
                var changePercent = prevQuote != null && prevQuote.Close > 0
                    ? Math.Round((todayQuote.Close - prevQuote.Close) / prevQuote.Close * 100, 2)
                    : 0m;

                newResults.Add(new DailyScreenResult
                {
                    StockId = stock.Id,
                    ScreenDate = date,
                    Close = todayQuote.Close,
                    Volume = todayQuote.Volume,
                    AvgVolume20 = Math.Round(avgVol20, 0),
                    MA20 = Math.Round(ma20, 2),
                    VolumeRatio = Math.Round((decimal)todayQuote.Volume / avgVol20, 2),
                    ChangePercent = changePercent
                });
            }
        }

        _db.DailyScreenResults.AddRange(newResults);
        await _db.SaveChangesAsync();
    }

    private async Task<DateOnly> GetLatestScreenDateAsync()
    {
        var dates = await GetAvailableDatesAsync(1);
        return dates.FirstOrDefault();
    }
}
