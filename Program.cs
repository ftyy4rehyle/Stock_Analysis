using Microsoft.EntityFrameworkCore;
using StockAnalysis.Data;
using StockAnalysis.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core SQLite
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "stock.db");
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// HttpClient for TWSE API
builder.Services.AddHttpClient<ITwseDataService, TwseDataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "StockAnalysis/1.0");
});

// Application services
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IScreeningService, ScreeningService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();

var app = builder.Build();

// Migrate DB & seed 股票主檔（從 TWSE 抓取真實股票代碼）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    db.Database.Migrate();

    var twse = scope.ServiceProvider.GetRequiredService<ITwseDataService>();
    await SeedData.InitializeAsync(db, twse);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
