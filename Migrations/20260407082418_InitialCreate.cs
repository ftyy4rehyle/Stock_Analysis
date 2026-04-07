using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalysis.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Market = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyQuotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockId = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Volume = table.Column<long>(type: "INTEGER", nullable: false),
                    Change = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ChangePercent = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyQuotes_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyScreenResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScreenDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Volume = table.Column<long>(type: "INTEGER", nullable: false),
                    AvgVolume20 = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MA20 = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    VolumeRatio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ChangePercent = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyScreenResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyScreenResults_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockId = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MA5 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MA20 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MA60 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    AvgVolume20 = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicators_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyQuotes_StockId_TradeDate",
                table: "DailyQuotes",
                columns: new[] { "StockId", "TradeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyScreenResults_StockId_ScreenDate",
                table: "DailyScreenResults",
                columns: new[] { "StockId", "ScreenDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Symbol",
                table: "Stocks",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicators_StockId_TradeDate",
                table: "TechnicalIndicators",
                columns: new[] { "StockId", "TradeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyQuotes");

            migrationBuilder.DropTable(
                name: "DailyScreenResults");

            migrationBuilder.DropTable(
                name: "TechnicalIndicators");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
