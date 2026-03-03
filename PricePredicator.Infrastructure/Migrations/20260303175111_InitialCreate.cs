using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PricePredicator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Volatility_Gold",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LogarithmicReturn = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol5 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol15 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol60 = table.Column<double>(type: "double precision", nullable: false),
                    ShortPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    LongPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volatility_Gold", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_NaturalGas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LogarithmicReturn = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol5 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol15 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol60 = table.Column<double>(type: "double precision", nullable: false),
                    ShortPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    LongPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volatility_NaturalGas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_Oil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LogarithmicReturn = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol5 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol15 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol60 = table.Column<double>(type: "double precision", nullable: false),
                    ShortPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    LongPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volatility_Oil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_Silver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LogarithmicReturn = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol5 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol15 = table.Column<double>(type: "double precision", nullable: false),
                    RollingVol60 = table.Column<double>(type: "double precision", nullable: false),
                    ShortPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    LongPanicScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volatility_Silver", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Gold_Timestamp",
                table: "Volatility_Gold",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_NaturalGas_Timestamp",
                table: "Volatility_NaturalGas",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Oil_Timestamp",
                table: "Volatility_Oil",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Silver_Timestamp",
                table: "Volatility_Silver",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Volatility_Gold");

            migrationBuilder.DropTable(
                name: "Volatility_NaturalGas");

            migrationBuilder.DropTable(
                name: "Volatility_Oil");

            migrationBuilder.DropTable(
                name: "Volatility_Silver");
        }
    }
}
