using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public class BacktestService : IBacktestService
{
    private readonly StockDbContext _db;

    public BacktestService(StockDbContext db) => _db = db;

    public async Task<BacktestViewModel> RunAsync(string symbol)
    {
        var stock = await _db.Stocks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Symbol == symbol && s.IsActive);

        if (stock == null)
            return new BacktestViewModel { ErrorMessage = $"找不到股票代號「{symbol}」" };

        // 讀取全部行情（時間序由舊到新）
        var quotes = await _db.DailyQuotes.AsNoTracking()
            .Where(q => q.StockId == stock.Id)
            .OrderBy(q => q.TradeDate)
            .ToListAsync();

        // 讀取技術指標（含買賣訊號）
        var indicatorMap = await _db.TechnicalIndicators.AsNoTracking()
            .Where(t => t.StockId == stock.Id)
            .ToDictionaryAsync(t => t.TradeDate);

        if (quotes.Count == 0 || indicatorMap.Count == 0)
            return new BacktestViewModel
            {
                Symbol = stock.Symbol,
                Name   = stock.Name,
                ErrorMessage = "尚無行情資料，請至資料同步頁面先同步該股票。"
            };

        // ── 回測模擬 ─────────────────────────────────────────────────────
        var trades    = new List<TradeResult>();
        bool inPos    = false;
        decimal buyPx = 0;
        DateOnly buyDt = default;

        foreach (var q in quotes)
        {
            indicatorMap.TryGetValue(q.TradeDate, out var ind);
            if (ind == null) continue;

            if (!inPos && ind.IsBuySignal)
            {
                // 買進：當日收盤價
                inPos  = true;
                buyPx  = q.Close;
                buyDt  = q.TradeDate;
            }
            else if (inPos && ind.IsSellSignal)
            {
                // 賣出：當日收盤價，記錄完成交易
                var profit = Math.Round((q.Close - buyPx) / buyPx * 100, 2);
                trades.Add(new TradeResult
                {
                    No            = trades.Count + 1,
                    BuyDate       = buyDt,
                    BuyPrice      = buyPx,
                    SellDate      = q.TradeDate,
                    SellPrice     = q.Close,
                    ProfitPercent = profit
                });
                inPos = false;
            }
        }

        // ── 摘要統計 ─────────────────────────────────────────────────────
        var summary = BuildSummary(trades, inPos, buyDt, buyPx);

        // ── 圖表資料 ─────────────────────────────────────────────────────
        var labels      = new List<string>();
        var profits     = new List<decimal>();
        var cumulative  = new List<decimal>();
        decimal cumFactor = 1m;

        foreach (var t in trades)
        {
            labels.Add($"#{t.No}");
            profits.Add(t.ProfitPercent);
            cumFactor *= 1 + t.ProfitPercent / 100m;
            cumulative.Add(Math.Round((cumFactor - 1) * 100, 2));
        }

        return new BacktestViewModel
        {
            Symbol      = stock.Symbol,
            Name        = stock.Name,
            Industry    = stock.Industry,
            DataFrom    = quotes.First().TradeDate,
            DataTo      = quotes.Last().TradeDate,
            Summary     = summary,
            Trades      = trades,
            TradeLabels      = labels,
            TradeProfits     = profits,
            CumulativeReturns = cumulative
        };
    }

    private static BacktestSummary BuildSummary(
        List<TradeResult> trades, bool inPos, DateOnly openDt, decimal openPx)
    {
        if (trades.Count == 0)
            return new BacktestSummary
            {
                HasOpenPosition = inPos,
                OpenBuyDate     = inPos ? openDt : null,
                OpenBuyPrice    = inPos ? openPx : null
            };

        int wins     = trades.Count(t => t.ProfitPercent > 0);
        var profits  = trades.Select(t => t.ProfitPercent).ToList();

        // 複利總報酬：Π(1 + Pi/100) - 1
        decimal cumFactor = trades.Aggregate(1m, (acc, t) => acc * (1 + t.ProfitPercent / 100m));
        decimal totalReturn = Math.Round((cumFactor - 1) * 100, 2);

        return new BacktestSummary
        {
            TotalTrades       = trades.Count,
            WinTrades         = wins,
            WinRate           = Math.Round((decimal)wins / trades.Count * 100, 1),
            AvgProfitPercent  = Math.Round(profits.Average(), 2),
            TotalProfitPercent = totalReturn,
            MaxProfit         = profits.Max(),
            MaxLoss           = profits.Min(),
            HasOpenPosition   = inPos,
            OpenBuyDate       = inPos ? openDt : null,
            OpenBuyPrice      = inPos ? openPx : null
        };
    }
}
