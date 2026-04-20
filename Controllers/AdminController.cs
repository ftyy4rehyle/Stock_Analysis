using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.Services;

namespace StockAnalysis.Controllers;

public class AdminController : Controller
{
    private readonly StockDbContext _db;
    private readonly ITwseDataService _twse;
    private readonly ILogger<AdminController> _logger;

    // 用來追蹤背景同步進度（簡易 in-process 方式）
    private static SyncStatus _syncStatus = new();

    public AdminController(StockDbContext db, ITwseDataService twse, ILogger<AdminController> logger)
    {
        _db = db;
        _twse = twse;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Sync()
    {
        var stockCount = await _db.Stocks.CountAsync();
        var quoteCount = await _db.DailyQuotes.CountAsync();
        var latestDate = await _db.DailyQuotes
            .OrderByDescending(q => q.TradeDate)
            .Select(q => (DateOnly?)q.TradeDate)
            .FirstOrDefaultAsync();

        var stocksWithData = await _db.DailyQuotes
            .Select(q => q.StockId)
            .Distinct()
            .CountAsync();

        ViewBag.StockCount     = stockCount;
        ViewBag.QuoteCount     = quoteCount;
        ViewBag.LatestDate     = latestDate?.ToString("yyyy/MM/dd") ?? "尚無資料";
        ViewBag.StocksWithData = stocksWithData;
        ViewBag.SyncStatus     = _syncStatus;

        return View();
    }

    /// <summary>同步單一股票近3個月行情（同步呼叫，等待完成）</summary>
    [HttpPost]
    public async Task<IActionResult> SyncOne(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("請提供股票代號");

        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
        if (stock == null)
            return NotFound($"找不到股票 {symbol}");

        await _twse.SyncStockAsync(symbol, monthsBack: 3);

        return Json(new { ok = true, message = $"{symbol} {stock.Name} 同步完成" });
    }

    /// <summary>批次同步成交量前 topN 檔（背景執行，立即回應）</summary>
    [HttpPost]
    public IActionResult SyncAll([FromForm] int topN = 50, [FromForm] int monthsBack = 3)
    {
        if (_syncStatus.IsRunning)
            return Json(new { ok = false, message = "同步中，請稍後再試" });

        _syncStatus = new SyncStatus { IsRunning = true, StartedAt = DateTime.Now, Log = new() };

        // 取得 scoped service 執行背景工作
        var scope = HttpContext.RequestServices.CreateScope();
        _ = Task.Run(async () =>
        {
            try
            {
                var twse = scope.ServiceProvider.GetRequiredService<ITwseDataService>();
                var progress = new Progress<string>(msg =>
                {
                    _syncStatus.Log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                    _syncStatus.LastMessage = msg;
                });

                await twse.SyncTopStocksAsync(topN, monthsBack, progress);
                _syncStatus.LastMessage = "批次同步完成！";
            }
            catch (Exception ex)
            {
                _syncStatus.LastMessage = $"發生錯誤：{ex.Message}";
                _logger.LogError(ex, "批次同步失敗");
            }
            finally
            {
                _syncStatus.IsRunning = false;
                _syncStatus.FinishedAt = DateTime.Now;
                scope.Dispose();
            }
        });

        return Json(new { ok = true, message = $"已開始背景同步前 {topN} 檔，請稍後重新整理查看進度" });
    }

    /// <summary>輪詢同步進度（AJAX 用）</summary>
    [HttpGet]
    public IActionResult SyncProgress()
    {
        return Json(new
        {
            isRunning    = _syncStatus.IsRunning,
            lastMessage  = _syncStatus.LastMessage,
            log          = _syncStatus.Log.TakeLast(20).ToList(),
            startedAt    = _syncStatus.StartedAt?.ToString("HH:mm:ss"),
            finishedAt   = _syncStatus.FinishedAt?.ToString("HH:mm:ss")
        });
    }

    /// <summary>重新從 TWSE 更新股票主檔（不影響行情資料）</summary>
    [HttpPost]
    public async Task<IActionResult> RefreshStockList()
    {
        var list = await _twse.FetchStockListAsync();
        if (list.Count == 0)
            return Json(new { ok = false, message = "無法連線 TWSE，請稍後再試" });

        int added = 0, updated = 0;
        foreach (var info in list)
        {
            var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == info.Symbol);
            if (stock == null)
            {
                _db.Stocks.Add(new Models.Stock
                {
                    Symbol = info.Symbol, Name = info.ShortName,
                    Industry = info.Industry, Market = "TWSE", IsActive = true
                });
                added++;
            }
            else
            {
                stock.Name = info.ShortName;
                stock.Industry = info.Industry;
                updated++;
            }
        }

        await _db.SaveChangesAsync();
        return Json(new { ok = true, message = $"股票主檔更新完成：新增 {added} 檔，更新 {updated} 檔，共 {list.Count} 檔" });
    }

    /// <summary>修正 DB 中仍為數字代碼的產業別欄位 → 中文名稱</summary>
    [HttpPost]
    public async Task<IActionResult> FixIndustryNames()
    {
        var stocks = await _db.Stocks.ToListAsync();
        int fixed_ = 0;

        foreach (var s in stocks)
        {
            if (TwseIndustryHelper.IsCode(s.Industry))
            {
                s.Industry = TwseIndustryHelper.Resolve(s.Industry);
                fixed_++;
            }
        }

        await _db.SaveChangesAsync();
        return Json(new { ok = true, message = $"產業別修正完成，共修正 {fixed_} 筆" });
    }
}

public class SyncStatus
{
    public bool IsRunning { get; set; }
    public string? LastMessage { get; set; }
    public List<string> Log { get; set; } = new();
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
