﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredictor.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateNewsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'PublishedAtUtc') THEN
                        ALTER TABLE ""ArticleLinks"" RENAME COLUMN ""PublishedAtUtc"" TO ""ReadAt"";
                    END IF;
                END
                $$;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ArticleLinks""
                ADD COLUMN IF NOT EXISTS ""IsProcessed"" boolean NOT NULL DEFAULT FALSE;");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'ExtractedAtUtc')
                       AND EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'IsTradeUseful') THEN
                        UPDATE ""ArticleLinks""
                        SET ""IsProcessed"" = CASE
                            WHEN ""ExtractedAtUtc"" IS NOT NULL OR ""IsTradeUseful"" THEN TRUE
                            ELSE FALSE
                        END;
                    END IF;
                END
                $$;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Articles"" (
                    ""Id"" uuid NOT NULL,
                    ""ArticleLinkId"" uuid NOT NULL,
                    ""IsTradingUseful"" boolean NULL,
                    ""ScannedAtUtc"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Articles"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Articles_ArticleLinks_ArticleLinkId""
                        FOREIGN KEY (""ArticleLinkId"") REFERENCES ""ArticleLinks"" (""Id"") ON DELETE RESTRICT
                );");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'ExtractedAtUtc')
                       AND EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'IsTradeUseful')
                       AND EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks'
                          AND column_name = 'ReadAt') THEN
                        INSERT INTO ""Articles"" (""Id"", ""ArticleLinkId"", ""IsTradingUseful"", ""ScannedAtUtc"")
                        SELECT
                            l.""Id"",
                            l.""Id"",
                            CASE
                                WHEN l.""ExtractedAtUtc"" IS NULL AND NOT l.""IsTradeUseful"" THEN NULL
                                ELSE l.""IsTradeUseful""
                            END,
                            COALESCE(l.""ExtractedAtUtc"", l.""ReadAt"")
                        FROM ""ArticleLinks"" AS l
                        WHERE l.""IsProcessed""
                          AND NOT EXISTS (
                              SELECT 1
                              FROM ""Articles"" AS a
                              WHERE a.""ArticleLinkId"" = l.""Id"");
                    END IF;
                END
                $$;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Articles_ArticleLinkId""
                ON ""Articles"" (""ArticleLinkId"");");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ArticleLinks"" DROP COLUMN IF EXISTS ""ExtractedAtUtc"";");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ArticleLinks"" DROP COLUMN IF EXISTS ""IsTradeUseful"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExtractedAtUtc",
                table: "ArticleLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTradeUseful",
                table: "ArticleLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE \"ArticleLinks\" AS l
                SET
                    \"ExtractedAtUtc\" = a.\"ScannedAtUtc\",
                    \"IsTradeUseful\" = COALESCE(a.\"IsTradingUseful\", FALSE)
                FROM \"Articles\" AS a
                WHERE a.\"ArticleLinkId\" = l.\"Id\";
                """);

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "ArticleLinks");

            migrationBuilder.RenameColumn(
                name: "ReadAt",
                table: "ArticleLinks",
                newName: "PublishedAtUtc");
        }
    }
}
