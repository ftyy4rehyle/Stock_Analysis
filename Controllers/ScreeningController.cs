using Microsoft.AspNetCore.Mvc;
using StockAnalysis.Services;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Controllers;

public class ScreeningController : Controller
{
    private readonly IScreeningService _screeningService;
    private readonly IExcelExportService _excelService;

    public ScreeningController(IScreeningService screeningService, IExcelExportService excelService)
    {
        _screeningService = screeningService;
        _excelService = excelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? date)
    {
        DateOnly? selectedDate = null;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsed))
            selectedDate = parsed;

        var availableDates = await _screeningService.GetAvailableDatesAsync(30);
        var results = await _screeningService.GetScreeningResultsAsync(selectedDate);

        var vm = new ScreeningPageViewModel
        {
            SelectedDate = selectedDate ?? availableDates.FirstOrDefault(),
            AvailableDates = availableDates,
            Results = results
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? date)
    {
        DateOnly? selectedDate = null;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsed))
            selectedDate = parsed;

        var results = await _screeningService.GetScreeningResultsAsync(selectedDate);
        var exportDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        var bytes = _excelService.ExportScreeningResults(results, exportDate);
        var fileName = $"台股篩選_{exportDate:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
