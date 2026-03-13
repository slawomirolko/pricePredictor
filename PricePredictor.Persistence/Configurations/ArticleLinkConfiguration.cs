using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricePredictor.Application.Models;

namespace PricePredictor.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ArticleLink entity.
/// </summary>
public sealed class ArticleLinkConfiguration : IEntityTypeConfiguration<ArticleLink>
{
    public void Configure(EntityTypeBuilder<ArticleLink> builder)
    {
        builder.ToTable("ArticleLinks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Url).IsRequired();
        builder.Property(e => e.PublishedAtUtc).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.ExtractedAtUtc).IsRequired(false);
        builder.Property(e => e.IsTradeUseful).IsRequired();
        builder.HasIndex(e => e.Url).IsUnique();
    }
}
