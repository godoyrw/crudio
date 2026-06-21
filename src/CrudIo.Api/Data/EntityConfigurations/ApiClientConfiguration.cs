using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Data.EntityConfigurations;

public sealed class ApiClientConfiguration
    : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.ToTable("api_clients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.ClientId)
            .IsUnique();

        builder.Property(x => x.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.RevokedAt)
            .IsRequired(false);
    }
}
