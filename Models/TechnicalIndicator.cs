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

    public Stock Stock { get; set; } = null!;
}
