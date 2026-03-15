using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredictor.Persistence.Migrations;

public partial class EnsureGoldNewsEmbeddingArticleUpsert : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS gold_news_embeddings
    ADD COLUMN IF NOT EXISTS article_id uuid,
    ADD COLUMN IF NOT EXISTS read_at_utc timestamptz,
    ADD COLUMN IF NOT EXISTS summary text;");

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
CREATE UNIQUE INDEX IF NOT EXISTS ix_gold_news_embeddings_article_id
ON gold_news_embeddings (article_id);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_gold_news_embeddings_article_id;");
    }
}

