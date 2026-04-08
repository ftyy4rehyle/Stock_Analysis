using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalysis.Models;

public class TechnicalIndicator
{
    public int Id { get; set; }

    public int StockId { get; set; }

    public DateOnly TradeDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MA5 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MA20 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MA60 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? AvgVolume20 { get; set; }  // 20日均量（張）

    /// <summary>買進訊號：Close>MA20 且 MA20上升 且 MA5>MA20 且前日未觸發</summary>
    public bool IsBuySignal { get; set; }

    /// <summary>賣出訊號：連續2日Close&lt;MA20 且前日未觸發</summary>
    public bool IsSellSignal { get; set; }

    public Stock Stock { get; set; } = null!;
}
