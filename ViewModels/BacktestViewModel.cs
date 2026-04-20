namespace StockAnalysis.ViewModels;

public class TradeResult
{
    public int No { get; set; }
    public DateOnly BuyDate { get; set; }
    public decimal BuyPrice { get; set; }
    public DateOnly SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public decimal ProfitPercent { get; set; }  // (SellPrice - BuyPrice) / BuyPrice * 100
}

public class BacktestSummary
{
    public int TotalTrades { get; set; }
    public int WinTrades { get; set; }
    public decimal WinRate { get; set; }          // %
    public decimal AvgProfitPercent { get; set; } // 算術平均
    public decimal TotalProfitPercent { get; set; } // 複利累積報酬
    public decimal MaxProfit { get; set; }
    public decimal MaxLoss { get; set; }
    public bool HasOpenPosition { get; set; }
    public DateOnly? OpenBuyDate { get; set; }
    public decimal? OpenBuyPrice { get; set; }
}

public class BacktestViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public DateOnly? DataFrom { get; set; }
    public DateOnly? DataTo { get; set; }

    public BacktestSummary Summary { get; set; } = new();
    public List<TradeResult> Trades { get; set; } = new();

    // 給圖表用：每筆交易的單筆報酬與累積報酬
    public List<string> TradeLabels { get; set; } = new();        // "第N筆"
    public List<decimal> TradeProfits { get; set; } = new();      // 各筆報酬%
    public List<decimal> CumulativeReturns { get; set; } = new(); // 複利累積%

    public string? ErrorMessage { get; set; }
    public bool HasData => Trades.Any();
}
