using Microsoft.AspNetCore.Mvc;
using StockAnalysis.Services;

namespace StockAnalysis.Controllers;

public class BacktestController : Controller
{
    private readonly IBacktestService _backtest;
    private readonly IStockService    _stock;

    public BacktestController(IBacktestService backtest, IStockService stock)
    {
        _backtest = backtest;
        _stock    = stock;
    }

    // GET /Backtest
    [HttpGet]
    public IActionResult Index() => View();

    // GET /Backtest/Detail/2330
    [HttpGet("Backtest/Detail/{symbol}")]
    public async Task<IActionResult> Detail(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return RedirectToAction(nameof(Index));

        var vm = await _backtest.RunAsync(symbol.Trim().ToUpper());
        return View(vm);
    }

    // POST /Backtest  (表單送出 → 跳轉 Detail)
    [HttpPost]
    public IActionResult Search(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return View(nameof(Index));

        return RedirectToAction(nameof(Detail), new { symbol = symbol.Trim() });
    }

    // AJAX 自動完成（沿用 StockController 相同 endpoint 格式）
    [HttpGet]
    public async Task<IActionResult> SearchJson(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<object>());

        var results = await _stock.SearchStocksAsync(q);
        return Json(results.Select(r => new { r.Symbol, r.Name, label = $"{r.Symbol} {r.Name}" }));
    }
}
