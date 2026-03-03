using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredicator.Infrastructure.Migrations;

[DbContext(typeof(Data.PricePredictorDbContext))]
[Migration("20260303193500_RenameVolatilityTablesToCommodities")]
public partial class RenameVolatilityTablesToCommodities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "Volatility_Gold",
            newName: "Gold");

        migrationBuilder.RenameTable(
            name: "Volatility_Silver",
            newName: "Silver");

        migrationBuilder.RenameTable(
            name: "Volatility_NaturalGas",
            newName: "NaturalGas");

        migrationBuilder.RenameTable(
            name: "Volatility_Oil",
            newName: "Oil");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Gold_Timestamp",
            table: "Gold",
            newName: "IX_Gold_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Silver_Timestamp",
            table: "Silver",
            newName: "IX_Silver_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_NaturalGas_Timestamp",
            table: "NaturalGas",
            newName: "IX_NaturalGas_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Oil_Timestamp",
            table: "Oil",
            newName: "IX_Oil_Timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameIndex(
            name: "IX_Gold_Timestamp",
            table: "Gold",
            newName: "IX_Volatility_Gold_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Silver_Timestamp",
            table: "Silver",
            newName: "IX_Volatility_Silver_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "NaturalGas",
            newName: "IX_Volatility_NaturalGas_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Oil_Timestamp",
            table: "Oil",
            newName: "IX_Volatility_Oil_Timestamp");

        migrationBuilder.RenameTable(
            name: "Gold",
            newName: "Volatility_Gold");

        migrationBuilder.RenameTable(
            name: "Silver",
            newName: "Volatility_Silver");

        migrationBuilder.RenameTable(
            name: "NaturalGas",
            newName: "Volatility_NaturalGas");

        migrationBuilder.RenameTable(
            name: "Oil",
            newName: "Volatility_Oil");
    }
}
