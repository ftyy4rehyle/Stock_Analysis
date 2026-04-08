using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalysis.Migrations
{
    /// <inheritdoc />
    public partial class AddBuySellSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBuySignal",
                table: "TechnicalIndicators",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSellSignal",
                table: "TechnicalIndicators",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBuySignal",
                table: "TechnicalIndicators");

            migrationBuilder.DropColumn(
                name: "IsSellSignal",
                table: "TechnicalIndicators");
        }
    }
}
