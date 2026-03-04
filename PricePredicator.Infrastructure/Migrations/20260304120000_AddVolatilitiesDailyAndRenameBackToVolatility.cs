using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PricePredicator.Infrastructure.Migrations;

public partial class AddVolatilitiesDailyAndRenameBackToVolatility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.RenameIndex(
            name: "IX_Gold_Timestamp",
            table: "Volatility_Gold",
            newName: "IX_Volatility_Gold_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Silver_Timestamp",
            table: "Volatility_Silver",
            newName: "IX_Volatility_Silver_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_NaturalGas_Timestamp",
            table: "Volatility_NaturalGas",
            newName: "IX_Volatility_NaturalGas_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Oil_Timestamp",
            table: "Volatility_Oil",
            newName: "IX_Volatility_Oil_Timestamp");

        migrationBuilder.CreateTable(
            name: "Commodities",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Commodities", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Commodities_Name",
            table: "Commodities",
            column: "Name",
            unique: true);

        migrationBuilder.InsertData(
            table: "Commodities",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
                { 1, "Gold" },
                { 2, "Silver" },
                { 3, "NaturalGas" },
                { 4, "Oil" }
            });

        migrationBuilder.CreateTable(
            name: "Volatilities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CommodityId = table.Column<int>(type: "integer", nullable: false),
                Day = table.Column<DateOnly>(type: "date", nullable: false),
                Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Avg = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                VolumeSum = table.Column<long>(type: "bigint", nullable: false),
                RangePct = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Volatilities", x => x.Id);
                table.ForeignKey(
                    name: "FK_Volatilities_Commodities_CommodityId",
                    column: x => x.CommodityId,
                    principalTable: "Commodities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Volatilities_CommodityId_Day",
            table: "Volatilities",
            columns: new[] { "CommodityId", "Day" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Volatilities");

        migrationBuilder.DropTable(
            name: "Commodities");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Gold_Timestamp",
            table: "Volatility_Gold",
            newName: "IX_Gold_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Silver_Timestamp",
            table: "Volatility_Silver",
            newName: "IX_Silver_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_NaturalGas_Timestamp",
            table: "Volatility_NaturalGas",
            newName: "IX_NaturalGas_Timestamp");

        migrationBuilder.RenameIndex(
            name: "IX_Volatility_Oil_Timestamp",
            table: "Volatility_Oil",
            newName: "IX_Oil_Timestamp");

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
    }
}
