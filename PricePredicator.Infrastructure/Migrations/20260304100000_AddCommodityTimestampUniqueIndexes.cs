using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredicator.Infrastructure.Migrations;

public partial class AddCommodityTimestampUniqueIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"Gold\" a USING \"Gold\" b WHERE a.\"Timestamp\" = b.\"Timestamp\" AND a.\"Id\" < b.\"Id\";");
        migrationBuilder.Sql("DELETE FROM \"Silver\" a USING \"Silver\" b WHERE a.\"Timestamp\" = b.\"Timestamp\" AND a.\"Id\" < b.\"Id\";");
        migrationBuilder.Sql("DELETE FROM \"NaturalGas\" a USING \"NaturalGas\" b WHERE a.\"Timestamp\" = b.\"Timestamp\" AND a.\"Id\" < b.\"Id\";");
        migrationBuilder.Sql("DELETE FROM \"Oil\" a USING \"Oil\" b WHERE a.\"Timestamp\" = b.\"Timestamp\" AND a.\"Id\" < b.\"Id\";");

        migrationBuilder.DropIndex(
            name: "IX_Gold_Timestamp",
            table: "Gold");

        migrationBuilder.DropIndex(
            name: "IX_Silver_Timestamp",
            table: "Silver");

        migrationBuilder.DropIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "NaturalGas");

        migrationBuilder.DropIndex(
            name: "IX_Oil_Timestamp",
            table: "Oil");

        migrationBuilder.CreateIndex(
            name: "IX_Gold_Timestamp",
            table: "Gold",
            column: "Timestamp",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Silver_Timestamp",
            table: "Silver",
            column: "Timestamp",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "NaturalGas",
            column: "Timestamp",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Oil_Timestamp",
            table: "Oil",
            column: "Timestamp",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Gold_Timestamp",
            table: "Gold");

        migrationBuilder.DropIndex(
            name: "IX_Silver_Timestamp",
            table: "Silver");

        migrationBuilder.DropIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "NaturalGas");

        migrationBuilder.DropIndex(
            name: "IX_Oil_Timestamp",
            table: "Oil");

        migrationBuilder.CreateIndex(
            name: "IX_Gold_Timestamp",
            table: "Gold",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_Silver_Timestamp",
            table: "Silver",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "NaturalGas",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_Oil_Timestamp",
            table: "Oil",
            column: "Timestamp");
    }
}

