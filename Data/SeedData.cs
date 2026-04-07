using StockAnalysis.Models;
using StockAnalysis.Services;

namespace StockAnalysis.Data;

public static class SeedData
{
    /// <summary>
    /// 從 TWSE 抓取真實上市股票清單填入 Stocks 主檔。
    /// 行情資料需另至管理頁面觸發同步。
    /// </summary>
    public static async Task InitializeAsync(StockDbContext context, ITwseDataService twseService)
    {
        if (context.Stocks.Any()) return;

        var list = await twseService.FetchStockListAsync();

        if (list.Count == 0)
        {
            // TWSE API 無法連線時，至少建立幾筆基本資料讓系統可以啟動
            var fallback = new[]
            {
                new Stock { Symbol = "2330", Name = "台積電",  Industry = "半導體業",   Market = "TWSE" },
                new Stock { Symbol = "2317", Name = "鴻海",    Industry = "電子零組件業", Market = "TWSE" },
                new Stock { Symbol = "2454", Name = "聯發科",  Industry = "半導體業",   Market = "TWSE" },
                new Stock { Symbol = "2412", Name = "中華電",  Industry = "通信網路業",  Market = "TWSE" },
                new Stock { Symbol = "2882", Name = "國泰金",  Industry = "金融業",     Market = "TWSE" },
            };
            context.Stocks.AddRange(fallback);
            await context.SaveChangesAsync();
            return;
        }

        var stocks = list.Select(s => new Stock
        {
            Symbol   = s.Symbol,
            Name     = s.ShortName,
            Industry = s.Industry,
            Market   = "TWSE",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();
    }
}
