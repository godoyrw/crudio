using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
    public DbSet<ClientRefreshToken> ClientRefreshTokens => Set<ClientRefreshToken>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
