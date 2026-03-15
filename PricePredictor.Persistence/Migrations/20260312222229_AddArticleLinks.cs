﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredictor.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'ArticleLinks') THEN
                        CREATE TABLE ""ArticleLinks"" (
                            ""Id"" uuid NOT NULL,
                            ""Url"" text NOT NULL,
                            ""PublishedAtUtc"" timestamp with time zone NOT NULL,
                            ""Source"" text NOT NULL,
                            ""ExtractedAtUtc"" timestamp with time zone NULL,
                            ""IsTradeUseful"" boolean NOT NULL,
                            CONSTRAINT ""PK_ArticleLinks"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END
                $$;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ArticleLinks_Url""
                ON ""ArticleLinks"" (""Url"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""ArticleLinks"";");
        }
    }
}
