using System.ComponentModel.DataAnnotations;

namespace StockAnalysis.Models;

public class Stock
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Industry { get; set; }

    [StringLength(50)]
    public string? Market { get; set; }  // TWSE / TPEx

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DailyQuote> DailyQuotes { get; set; } = new List<DailyQuote>();
    public ICollection<TechnicalIndicator> TechnicalIndicators { get; set; } = new List<TechnicalIndicator>();
    public ICollection<DailyScreenResult> DailyScreenResults { get; set; } = new List<DailyScreenResult>();
}
