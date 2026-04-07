using ClosedXML.Excel;
using StockAnalysis.ViewModels;

namespace StockAnalysis.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportStockDetail(StockDetailViewModel model)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"{model.Symbol} {model.Name}");

        // 標題
        ws.Cell(1, 1).Value = $"{model.Symbol} {model.Name} - 近期行情";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 7).Merge();

        // 欄位標題
        var headers = new[] { "日期", "收盤價", "漲跌幅(%)", "成交量(張)", "MA5", "MA20", "MA60" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
            ws.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.SteelBlue;
            ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(2, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // 資料
        int row = 3;
        foreach (var point in model.ChartData)
        {
            ws.Cell(row, 1).Value = point.Date;
            ws.Cell(row, 2).Value = (double)point.Close;
            ws.Cell(row, 3).Value = (double)point.ChangePercent;
            ws.Cell(row, 4).Value = point.Volume;
            ws.Cell(row, 5).Value = point.MA5.HasValue ? (double?)point.MA5.Value : null;
            ws.Cell(row, 6).Value = point.MA20.HasValue ? (double?)point.MA20.Value : null;
            ws.Cell(row, 7).Value = point.MA60.HasValue ? (double?)point.MA60.Value : null;

            // 漲跌顏色
            if (point.ChangePercent > 0)
                ws.Cell(row, 3).Style.Font.FontColor = XLColor.Red;
            else if (point.ChangePercent < 0)
                ws.Cell(row, 3).Style.Font.FontColor = XLColor.Green;

            if (row % 2 == 0)
                ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(2);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportScreeningResults(List<ScreeningResultViewModel> results, DateOnly date)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("每日篩選結果");

        // 標題
        ws.Cell(1, 1).Value = $"台股盤後篩選 - {date:yyyy/MM/dd}（量>20日均量×1.5 且 收盤>MA20）";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 8).Merge();

        // 欄位標題
        var headers = new[] { "股票代號", "股票名稱", "產業", "收盤價", "漲跌幅(%)", "成交量(張)", "20日均量(張)", "量比" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
            ws.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(2, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // 資料
        int row = 3;
        foreach (var r in results)
        {
            ws.Cell(row, 1).Value = r.Symbol;
            ws.Cell(row, 2).Value = r.Name;
            ws.Cell(row, 3).Value = r.Industry ?? "-";
            ws.Cell(row, 4).Value = (double)r.Close;
            ws.Cell(row, 5).Value = (double)r.ChangePercent;
            ws.Cell(row, 6).Value = r.Volume;
            ws.Cell(row, 7).Value = (double)r.AvgVolume20;
            ws.Cell(row, 8).Value = (double)r.VolumeRatio;

            if (r.ChangePercent > 0)
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
            else if (r.ChangePercent < 0)
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.Green;

            if (row % 2 == 0)
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
        }

        ws.Cell(row + 1, 1).Value = $"共 {results.Count} 筆，匯出時間：{DateTime.Now:yyyy/MM/dd HH:mm}";
        ws.Cell(row + 1, 1).Style.Font.Italic = true;
        ws.Range(row + 1, 1, row + 1, 8).Merge();

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(2);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
