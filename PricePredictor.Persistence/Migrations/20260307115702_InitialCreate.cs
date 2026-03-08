using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PricePredictor.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Volatility_Gold",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommodityId = table.Column<int>(type: "integer", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Volatility_Gold_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_NaturalGas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommodityId = table.Column<int>(type: "integer", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Volatility_NaturalGas_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_Oil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommodityId = table.Column<int>(type: "integer", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Volatility_Oil_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Volatility_Silver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommodityId = table.Column<int>(type: "integer", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Volatility_Silver_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commodities_Name",
                table: "Commodities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volatilities_CommodityId_Day",
                table: "Volatilities",
                columns: new[] { "CommodityId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Gold_CommodityId",
                table: "Volatility_Gold",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Gold_Timestamp",
                table: "Volatility_Gold",
                column: "Timestamp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_NaturalGas_CommodityId",
                table: "Volatility_NaturalGas",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_NaturalGas_Timestamp",
                table: "Volatility_NaturalGas",
                column: "Timestamp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Oil_CommodityId",
                table: "Volatility_Oil",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Oil_Timestamp",
                table: "Volatility_Oil",
                column: "Timestamp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Silver_CommodityId",
                table: "Volatility_Silver",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Silver_Timestamp",
                table: "Volatility_Silver",
                column: "Timestamp",
                unique: true);

            // Keep pgvector storage in the baseline schema for Gold news embeddings.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS gold_news_embeddings (
                    id uuid PRIMARY KEY,
                    url text NOT NULL UNIQUE,
                    content text NOT NULL,
                    embedding vector(3072) NOT NULL,
                    created_at_utc timestamptz NOT NULL
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_gold_news_embeddings_created_at_utc
                ON gold_news_embeddings (created_at_utc DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_gold_news_embeddings_created_at_utc;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS gold_news_embeddings;");

            migrationBuilder.DropTable(
                name: "Volatilities");

            migrationBuilder.DropTable(
                name: "Volatility_Gold");

            migrationBuilder.DropTable(
                name: "Volatility_NaturalGas");

            migrationBuilder.DropTable(
                name: "Volatility_Oil");

            migrationBuilder.DropTable(
                name: "Volatility_Silver");

            migrationBuilder.DropTable(
                name: "Commodities");
        }
    }
}

