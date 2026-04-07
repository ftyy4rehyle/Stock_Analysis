using Microsoft.AspNetCore.Mvc;
using StockAnalysis.Services;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Controllers;

public class StockController : Controller
{
    private readonly IStockService _stockService;
    private readonly IExcelExportService _excelService;

    public StockController(IStockService stockService, IExcelExportService excelService)
    {
        _stockService = stockService;
        _excelService = excelService;
    }

    [HttpGet]
    public IActionResult Query(string? symbol, int days = 60)
    {
        var vm = new StockQueryViewModel { Symbol = symbol, Days = days };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Query(StockQueryViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Symbol))
        {
            form.ErrorMessage = "請輸入股票代號";
            return View(form);
        }

        var detail = await _stockService.GetStockDetailAsync(form.Symbol.Trim(), form.Days);
        if (detail == null)
        {
            form.ErrorMessage = $"找不到股票代號「{form.Symbol}」，請確認後重新查詢。";
            return View(form);
        }

        form.Detail = detail;
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string symbol, int days = 60)
    {
        var detail = await _stockService.GetStockDetailAsync(symbol, days);
        if (detail == null) return NotFound();

        var bytes = _excelService.ExportStockDetail(detail);
        var fileName = $"{symbol}_{detail.Name}_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new List<object>());

        var results = await _stockService.SearchStocksAsync(q);
        return Json(results.Select(r => new { r.Symbol, r.Name, label = $"{r.Symbol} {r.Name}" }));
    }
}
