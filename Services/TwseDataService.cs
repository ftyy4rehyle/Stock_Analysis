using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.Models;

namespace StockAnalysis.Services;

public class TwseDataService : ITwseDataService
{
    private readonly HttpClient _http;
    private readonly StockDbContext _db;
    private readonly ILogger<TwseDataService> _logger;

    // TWSE Open API endpoints
    private const string StockListUrl = "https://openapi.twse.com.tw/v1/opendata/t187ap03_L";
    private const string StockDayAllUrl = "https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL";
    private const string StockDayUrl = "https://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date={0}&stockNo={1}";

    public TwseDataService(HttpClient http, StockDbContext db, ILogger<TwseDataService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task<List<TwseStockInfo>> FetchStockListAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(StockListUrl);
            using var doc = JsonDocument.Parse(json);
            var result = new List<TwseStockInfo>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var code     = el.TryGetProperty("公司代號", out var c) ? c.GetString() ?? "" : "";
                var name     = el.TryGetProperty("公司簡稱", out var n) ? n.GetString() ?? "" : "";
                var rawInd   = el.TryGetProperty("產業別",   out var i) ? i.GetString() ?? "" : "";
                var industry = TwseIndustryHelper.Resolve(rawInd);  // 代碼 → 中文

                // 只取4碼純數字（普通股，排除ETF/受益憑證等）
                if (code.Length == 4 && code.All(char.IsDigit) && !string.IsNullOrEmpty(name))
                    result.Add(new TwseStockInfo(code, name, industry));
            }

            _logger.LogInformation("從 TWSE 取得 {Count} 檔上市股票", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "抓取 TWSE 股票清單失敗");
            return new List<TwseStockInfo>();
        }
    }

    /// <summary>從 STOCK_DAY_ALL 取得當日各股成交量，回傳代碼→成交量 mapping</summary>
    public async Task<Dictionary<string, long>> FetchTodayVolumeAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(StockDayAllUrl);
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, long>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var code = el.TryGetProperty("Code", out var c) ? c.GetString() ?? "" : "";
                if (!long.TryParse(
                    (el.TryGetProperty("TradeVolume", out var v) ? v.GetString() ?? "0" : "0").Replace(",", ""),
                    out var vol)) vol = 0;

                if (code.Length == 4 && code.All(char.IsDigit))
                    result[code] = vol;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "抓取今日成交量失敗，將使用代號排序");
            return new Dictionary<string, long>();
        }
    }

    public async Task<int> ImportMonthlyDataAsync(string symbol, int year, int month)
    {
        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
        if (stock == null) return 0;

        var dateParam = $"{year}{month:D2}01";
        var url = string.Format(StockDayUrl, dateParam, symbol);

        try
        {
            await Task.Delay(350); // 避免過快觸發 TWSE 限流
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("stat", out var stat) ||
                stat.GetString() != "OK") return 0;

            if (!doc.RootElement.TryGetProperty("data", out var dataArr)) return 0;

            var imported = 0;
            foreach (var row in dataArr.EnumerateArray())
            {
                var cells = row.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                if (cells.Length < 9) continue;

                // 日期：民國年/月/日 → 西元
                var dateParts = cells[0].Split('/');
                if (dateParts.Length != 3) continue;
                if (!int.TryParse(dateParts[0], out var rocYear)) continue;
                var tradeDate = new DateOnly(rocYear + 1911,
                    int.Parse(dateParts[1]), int.Parse(dateParts[2]));

                // 已存在則跳過
                if (await _db.DailyQuotes.AnyAsync(q => q.StockId == stock.Id && q.TradeDate == tradeDate))
                    continue;

                if (!TryParsePrice(cells[3], out var open)) continue;
                if (!TryParsePrice(cells[4], out var high)) continue;
                if (!TryParsePrice(cells[5], out var low)) continue;
                if (!TryParsePrice(cells[6], out var close)) continue;
                TryParsePrice(cells[7], out var change);

                // 成交股數（股）÷ 1000 = 張
                long.TryParse(cells[1].Replace(",", ""), out var shares);
                var volume = shares / 1000;

                var changePercent = (close - change) > 0
                    ? Math.Round(change / (close - change) * 100, 2)
                    : 0m;

                _db.DailyQuotes.Add(new DailyQuote
                {
                    StockId = stock.Id,
                    TradeDate = tradeDate,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    Change = change,
                    ChangePercent = changePercent
                });

                imported++;
            }

            await _db.SaveChangesAsync();
            return imported;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "抓取 {Symbol} {Year}/{Month} 行情失敗", symbol, year, month);
            return 0;
        }
    }

    public async Task SyncStockAsync(string symbol, int monthsBack = 3)
    {
        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
        if (stock == null) return;

        var today = DateTime.Today;
        var totalImported = 0;

        for (int i = monthsBack - 1; i >= 0; i--)
        {
            var target = today.AddMonths(-i);
            var count = await ImportMonthlyDataAsync(symbol, target.Year, target.Month);
            totalImported += count;
        }

        if (totalImported > 0)
        {
            await RecalculateIndicatorsAsync(stock.Id);
            _logger.LogInformation("{Symbol} 同步完成，共匯入 {Count} 筆", symbol, totalImported);
        }
    }

    public async Task SyncTopStocksAsync(int topN = 50, int monthsBack = 3, IProgress<string>? progress = null)
    {
        // 取得當日成交量排名
        var volumes = await FetchTodayVolumeAsync();

        var stocks = await _db.Stocks.Where(s => s.IsActive).ToListAsync();

        // 依成交量排序取前 topN，若無今日量資料則依代碼排序
        var targets = stocks
            .OrderByDescending(s => volumes.TryGetValue(s.Symbol, out var v) ? v : 0)
            .ThenBy(s => s.Symbol)
            .Take(topN)
            .ToList();

        for (int idx = 0; idx < targets.Count; idx++)
        {
            var stock = targets[idx];
            progress?.Report($"[{idx + 1}/{targets.Count}] 同步 {stock.Symbol} {stock.Name}...");

            try
            {
                await SyncStockAsync(stock.Symbol, monthsBack);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步 {Symbol} 失敗，繼續下一檔", stock.Symbol);
            }
        }

        progress?.Report($"批次同步完成，共 {targets.Count} 檔");
    }

    public async Task RecalculateIndicatorsAsync(int stockId)
    {
        // 刪除舊指標
        var oldIndicators = await _db.TechnicalIndicators.Where(t => t.StockId == stockId).ToListAsync();
        _db.TechnicalIndicators.RemoveRange(oldIndicators);

        var oldScreens = await _db.DailyScreenResults.Where(r => r.StockId == stockId).ToListAsync();
        _db.DailyScreenResults.RemoveRange(oldScreens);

        await _db.SaveChangesAsync();

        // 讀取所有行情（時間序）
        var quotes = await _db.DailyQuotes
            .Where(q => q.StockId == stockId)
            .OrderBy(q => q.TradeDate)
            .ToListAsync();

        if (quotes.Count == 0) return;

        var newIndicators = new List<TechnicalIndicator>();
        var newScreens    = new List<DailyScreenResult>();

        // ── Pass 1：計算 MA 指標 ────────────────────────────────────────
        for (int i = 0; i < quotes.Count; i++)
        {
            var q = quotes[i];

            decimal? ma5 = i >= 4
                ? quotes.Skip(i - 4).Take(5).Average(x => x.Close) : null;
            decimal? ma20 = i >= 19
                ? quotes.Skip(i - 19).Take(20).Average(x => x.Close) : null;
            decimal? ma60 = i >= 59
                ? quotes.Skip(i - 59).Take(60).Average(x => x.Close) : null;
            decimal? avgVol20 = i >= 19
                ? (decimal)quotes.Skip(i - 19).Take(20).Average(x => (double)x.Volume)
                : null;

            newIndicators.Add(new TechnicalIndicator
            {
                StockId     = stockId,
                TradeDate   = q.TradeDate,
                MA5         = ma5.HasValue     ? Math.Round(ma5.Value, 2)     : null,
                MA20        = ma20.HasValue    ? Math.Round(ma20.Value, 2)    : null,
                MA60        = ma60.HasValue    ? Math.Round(ma60.Value, 2)    : null,
                AvgVolume20 = avgVol20.HasValue ? Math.Round(avgVol20.Value, 0) : null
            });

            // 每日篩選（量 > 均量×1.5 且 Close > MA20）
            if (avgVol20.HasValue && ma20.HasValue &&
                q.Volume > avgVol20.Value * 1.5m &&
                q.Close > ma20.Value)
            {
                newScreens.Add(new DailyScreenResult
                {
                    StockId      = stockId,
                    ScreenDate   = q.TradeDate,
                    Close        = q.Close,
                    Volume       = q.Volume,
                    AvgVolume20  = Math.Round(avgVol20.Value, 0),
                    MA20         = Math.Round(ma20.Value, 2),
                    VolumeRatio  = Math.Round((decimal)q.Volume / avgVol20.Value, 2),
                    ChangePercent = q.ChangePercent
                });
            }
        }

        // ── Pass 2：計算買賣訊號（需要前日指標，故獨立一輪）───────────────
        // 買進訊號規則：
        //   Close > MA20
        //   MA20 > MA20[i-3]（均線向上）
        //   MA5 > MA20
        //   前一交易日 IsBuySignal == false（首次成立才標記）
        //
        // 賣出訊號規則：
        //   當日 Close < MA20
        //   前一交易日 Close < MA20（連續2日收在MA20下方）
        //   前一交易日 IsSellSignal == false（首次成立才標記）
        for (int i = 1; i < newIndicators.Count; i++)
        {
            var ind      = newIndicators[i];
            var prevInd  = newIndicators[i - 1];
            var q        = quotes[i];
            var prevQ    = quotes[i - 1];

            if (!ind.MA20.HasValue || !ind.MA5.HasValue) continue;

            // 買進訊號
            var ma20_3ago = i >= 3 ? newIndicators[i - 3].MA20 : null;
            if (ma20_3ago.HasValue &&
                q.Close > ind.MA20.Value &&
                ind.MA20.Value > ma20_3ago.Value &&
                ind.MA5.Value > ind.MA20.Value &&
                !prevInd.IsBuySignal)
            {
                ind.IsBuySignal = true;
            }

            // 賣出訊號
            if (prevInd.MA20.HasValue &&
                q.Close < ind.MA20.Value &&
                prevQ.Close < prevInd.MA20.Value &&
                !prevInd.IsSellSignal)
            {
                ind.IsSellSignal = true;
            }
        }

        _db.TechnicalIndicators.AddRange(newIndicators);
        _db.DailyScreenResults.AddRange(newScreens);
        await _db.SaveChangesAsync();
    }

    private static bool TryParsePrice(string s, out decimal value)
    {
        s = s.Replace(",", "").Trim();
        if (s == "--" || s == "" || s == "除權息")
        {
            value = 0;
            return false;
        }
        // 漲跌差前綴 +/- 保留
        return decimal.TryParse(s, out value);
    }
}
