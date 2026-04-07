using StockAnalysis.Models;

namespace StockAnalysis.Data;

public static class SeedData
{
    public static void Initialize(StockDbContext context)
    {
        if (context.Stocks.Any()) return;

        var stocks = new[]
        {
            new Stock { Symbol = "2330", Name = "台積電", Industry = "半導體", Market = "TWSE" },
            new Stock { Symbol = "2317", Name = "鴻海", Industry = "電子零組件", Market = "TWSE" },
            new Stock { Symbol = "2454", Name = "聯發科", Industry = "半導體", Market = "TWSE" },
            new Stock { Symbol = "2412", Name = "中華電", Industry = "電信", Market = "TWSE" },
            new Stock { Symbol = "2308", Name = "台達電", Industry = "電子零組件", Market = "TWSE" },
            new Stock { Symbol = "2882", Name = "國泰金", Industry = "金融", Market = "TWSE" },
            new Stock { Symbol = "1301", Name = "台塑", Industry = "塑膠", Market = "TWSE" },
            new Stock { Symbol = "2002", Name = "中鋼", Industry = "鋼鐵", Market = "TWSE" },
            new Stock { Symbol = "3008", Name = "大立光", Industry = "光學", Market = "TWSE" },
            new Stock { Symbol = "2379", Name = "瑞昱", Industry = "半導體", Market = "TWSE" },
        };

        context.Stocks.AddRange(stocks);
        context.SaveChanges();

        var random = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 產生近90日的每日行情 (含週末簡化處理)
        var tradeDates = Enumerable.Range(0, 90)
            .Select(i => today.AddDays(-i))
            .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            .OrderBy(d => d)
            .ToList();

        var basePrices = new Dictionary<string, decimal>
        {
            ["2330"] = 780m, ["2317"] = 168m, ["2454"] = 1250m, ["2412"] = 119m,
            ["2308"] = 298m, ["2882"] = 52m,  ["1301"] = 85m,   ["2002"] = 29m,
            ["3008"] = 2200m,["2379"] = 520m
        };

        var baseVolumes = new Dictionary<string, long>
        {
            ["2330"] = 25000, ["2317"] = 30000, ["2454"] = 8000,  ["2412"] = 5000,
            ["2308"] = 6000,  ["2882"] = 20000, ["1301"] = 8000,  ["2002"] = 15000,
            ["3008"] = 800,   ["2379"] = 5000
        };

        var quotes = new List<DailyQuote>();
        var allStockPrices = new Dictionary<int, List<(DateOnly date, decimal close, long volume)>>();

        foreach (var stock in stocks)
        {
            var price = basePrices[stock.Symbol];
            var vol = baseVolumes[stock.Symbol];
            var stockPrices = new List<(DateOnly, decimal, long)>();

            foreach (var date in tradeDates)
            {
                var change = (decimal)(random.NextDouble() * 0.06 - 0.03);
                price = Math.Max(price * (1 + change), 1m);
                price = Math.Round(price, 1);

                var volumeRand = (long)(vol * (0.5 + random.NextDouble()));
                // 偶爾產生大量日（用於篩選測試）
                if (random.NextDouble() > 0.85) volumeRand = (long)(vol * (1.6 + random.NextDouble()));

                var high = Math.Round(price * (1 + (decimal)(random.NextDouble() * 0.02)), 1);
                var low = Math.Round(price * (1 - (decimal)(random.NextDouble() * 0.02)), 1);
                var open = Math.Round(low + (high - low) * (decimal)random.NextDouble(), 1);

                quotes.Add(new DailyQuote
                {
                    StockId = stock.Id,
                    TradeDate = date,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = price,
                    Volume = volumeRand,
                    Change = 0,
                    ChangePercent = 0
                });

                stockPrices.Add((date, price, volumeRand));
            }

            allStockPrices[stock.Id] = stockPrices;
        }

        // 修正 Change / ChangePercent
        foreach (var stock in stocks)
        {
            var stockQuotes = quotes.Where(q => q.StockId == stock.Id).OrderBy(q => q.TradeDate).ToList();
            for (int i = 1; i < stockQuotes.Count; i++)
            {
                var prev = stockQuotes[i - 1].Close;
                var curr = stockQuotes[i].Close;
                stockQuotes[i].Change = Math.Round(curr - prev, 2);
                stockQuotes[i].ChangePercent = prev == 0 ? 0 : Math.Round((curr - prev) / prev * 100, 2);
            }
        }

        context.DailyQuotes.AddRange(quotes);
        context.SaveChanges();

        // 計算技術指標
        var indicators = new List<TechnicalIndicator>();
        var screenResults = new List<DailyScreenResult>();

        foreach (var stock in stocks)
        {
            var priceList = allStockPrices[stock.Id];

            for (int i = 0; i < priceList.Count; i++)
            {
                var (date, close, volume) = priceList[i];

                decimal? ma5 = i >= 4 ? priceList.Skip(i - 4).Take(5).Average(x => x.close) : null;
                decimal? ma20 = i >= 19 ? priceList.Skip(i - 19).Take(20).Average(x => x.close) : null;
                decimal? ma60 = i >= 59 ? priceList.Skip(i - 59).Take(60).Average(x => x.close) : null;
                decimal? avgVol20 = i >= 19 ? priceList.Skip(i - 19).Take(20).Average(x => (decimal)x.volume) : null;

                indicators.Add(new TechnicalIndicator
                {
                    StockId = stock.Id,
                    TradeDate = date,
                    MA5 = ma5.HasValue ? Math.Round(ma5.Value, 2) : null,
                    MA20 = ma20.HasValue ? Math.Round(ma20.Value, 2) : null,
                    MA60 = ma60.HasValue ? Math.Round(ma60.Value, 2) : null,
                    AvgVolume20 = avgVol20.HasValue ? Math.Round(avgVol20.Value, 0) : null
                });

                // 篩選條件：成交量 > 20日均量 * 1.5 且收盤價 > MA20
                if (avgVol20.HasValue && ma20.HasValue &&
                    volume > avgVol20.Value * 1.5m &&
                    close > ma20.Value)
                {
                    var changePercent = i > 0
                        ? Math.Round((close - priceList[i - 1].close) / priceList[i - 1].close * 100, 2)
                        : 0m;

                    screenResults.Add(new DailyScreenResult
                    {
                        StockId = stock.Id,
                        ScreenDate = date,
                        Close = close,
                        Volume = volume,
                        AvgVolume20 = Math.Round(avgVol20.Value, 0),
                        MA20 = Math.Round(ma20.Value, 2),
                        VolumeRatio = Math.Round((decimal)volume / avgVol20.Value, 2),
                        ChangePercent = changePercent
                    });
                }
            }
        }

        context.TechnicalIndicators.AddRange(indicators);
        context.DailyScreenResults.AddRange(screenResults);
        context.SaveChanges();
    }
}
