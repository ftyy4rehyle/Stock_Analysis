using Microsoft.EntityFrameworkCore;
using StockAnalysis.Models;

namespace StockAnalysis.Data;

public class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options) { }

    public DbSet<Stock> Stocks { get; set; }
    public DbSet<DailyQuote> DailyQuotes { get; set; }
    public DbSet<TechnicalIndicator> TechnicalIndicators { get; set; }
    public DbSet<DailyScreenResult> DailyScreenResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Stock>(e =>
        {
            e.HasIndex(s => s.Symbol).IsUnique();
            e.HasMany(s => s.DailyQuotes).WithOne(q => q.Stock).HasForeignKey(q => q.StockId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.TechnicalIndicators).WithOne(t => t.Stock).HasForeignKey(t => t.StockId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.DailyScreenResults).WithOne(r => r.Stock).HasForeignKey(r => r.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyQuote>(e =>
        {
            e.HasIndex(q => new { q.StockId, q.TradeDate }).IsUnique();
        });

        modelBuilder.Entity<TechnicalIndicator>(e =>
        {
            e.HasIndex(t => new { t.StockId, t.TradeDate }).IsUnique();
        });

        modelBuilder.Entity<DailyScreenResult>(e =>
        {
            e.HasIndex(r => new { r.StockId, r.ScreenDate }).IsUnique();
        });
    }
}
