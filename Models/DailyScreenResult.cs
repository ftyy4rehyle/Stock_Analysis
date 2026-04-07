using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalysis.Models;

public class DailyScreenResult
{
    public int Id { get; set; }

    public int StockId { get; set; }

    public DateOnly ScreenDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Close { get; set; }

    public long Volume { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AvgVolume20 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MA20 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal VolumeRatio { get; set; }  // Volume / AvgVolume20

    [Column(TypeName = "decimal(6,2)")]
    public decimal ChangePercent { get; set; }

    public Stock Stock { get; set; } = null!;
}
