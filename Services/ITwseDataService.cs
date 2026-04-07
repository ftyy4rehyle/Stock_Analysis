using StockAnalysis.Models;

namespace StockAnalysis.Services;

public interface ITwseDataService
{
    /// <summary>從 TWSE Open API 抓取上市公司清單</summary>
    Task<List<TwseStockInfo>> FetchStockListAsync();

    /// <summary>抓取指定股票指定月份的日行情並存入 DB</summary>
    Task<int> ImportMonthlyDataAsync(string symbol, int year, int month);

    /// <summary>抓取指定股票近 N 個月歷史行情，並重算技術指標與篩選結果</summary>
    Task SyncStockAsync(string symbol, int monthsBack = 3);

    /// <summary>批次同步多檔股票（依當日成交量排前 topN 檔）</summary>
    Task SyncTopStocksAsync(int topN = 50, int monthsBack = 3, IProgress<string>? progress = null);

    /// <summary>重新計算指定股票的技術指標與篩選結果</summary>
    Task RecalculateIndicatorsAsync(int stockId);
}

public record TwseStockInfo(string Symbol, string ShortName, string Industry);
