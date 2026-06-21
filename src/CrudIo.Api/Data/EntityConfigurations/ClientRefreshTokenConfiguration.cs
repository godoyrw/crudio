using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Data.EntityConfigurations;

public sealed class ClientRefreshTokenConfiguration
    : IEntityTypeConfiguration<ClientRefreshToken>
{
    public void Configure(EntityTypeBuilder<ClientRefreshToken> builder)
    {
        builder.ToTable("client_refresh_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .IsRequired(false);

        builder.Property(x => x.UsedAt)
            .IsRequired(false);

        builder.Property(x => x.ReplacedByTokenId)
            .IsRequired(false);

        builder.HasOne(x => x.ApiClient)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.ApiClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
