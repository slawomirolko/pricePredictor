using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PricePredictor.Persistence.Migrations;

[DbContext(typeof(PricePredictorDbContext))]
[Migration("20260315103000_EnsureGoldNewsEmbeddingArticleIdUniqueIndex")]
public partial class EnsureGoldNewsEmbeddingArticleIdUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
WITH ranked AS (
    SELECT ctid,
           ROW_NUMBER() OVER (PARTITION BY article_id ORDER BY created_at_utc DESC, id DESC) AS rn
    FROM gold_news_embeddings
    WHERE article_id IS NOT NULL
)
DELETE FROM gold_news_embeddings AS g
USING ranked AS r
WHERE g.ctid = r.ctid
  AND r.rn > 1;");

        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_gold_news_embeddings_article_id;
CREATE UNIQUE INDEX IF NOT EXISTS ix_gold_news_embeddings_article_id
ON gold_news_embeddings (article_id);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_gold_news_embeddings_article_id;");
    }
}
