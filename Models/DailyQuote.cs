using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalysis.Models;

public class DailyQuote
{
    public int Id { get; set; }

    public int StockId { get; set; }

    public DateOnly TradeDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Open { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal High { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Low { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Close { get; set; }

    public long Volume { get; set; }  // 成交量（張）

    [Column(TypeName = "decimal(10,2)")]
    public decimal Change { get; set; }  // 漲跌

    [Column(TypeName = "decimal(6,2)")]
    public decimal ChangePercent { get; set; }  // 漲跌幅%

    public Stock Stock { get; set; } = null!;
}
