using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PricePredictor.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PricePredictorDbContext))]
    [Migration("20260313130000_UpdateGoldNewsEmbeddingDimensionTo5120")]
    public partial class UpdateGoldNewsEmbeddingDimensionTo5120 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS gold_news_embeddings
                ALTER COLUMN embedding TYPE vector(5120);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS gold_news_embeddings
                ALTER COLUMN embedding TYPE vector(3072);
            ");
        }
    }
}
