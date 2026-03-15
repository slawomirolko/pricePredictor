using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricePredictor.Application.Models;

namespace PricePredictor.Persistence.Configurations;

public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ArticleLinkId).IsRequired();
        builder.Property(e => e.IsTradingUseful).IsRequired(false);
        builder.Property(e => e.ScannedAtUtc).IsRequired();
        builder.Property(e => e.Summary).IsRequired(false);
        builder.HasIndex(e => e.ArticleLinkId).IsUnique();

        builder.HasOne<ArticleLink>()
            .WithOne()
            .HasForeignKey<Article>(e => e.ArticleLinkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
