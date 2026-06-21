using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Data;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Common.Security;

public static class ApiClientSeeder
{
    public static async Task SeedFromEnvironmentAsync(IServiceProvider serviceProvider)
    {
        var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
        var apiKey = Environment.GetEnvironmentVariable("CLIENT_API_KEY");
        var name = Environment.GetEnvironmentVariable("CLIENT_NAME") ?? "Default API Client";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey))
            return;

        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var passwordService = serviceProvider.GetRequiredService<IPasswordService>();

        var apiClient = await dbContext.ApiClients
            .FirstOrDefaultAsync(x => x.ClientId == clientId);

        if (apiClient is null)
        {
            apiClient = new ApiClient
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ApiKeyHash = passwordService.Hash(apiKey),
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.ApiClients.Add(apiClient);
        }
        else
        {
            apiClient.ApiKeyHash = passwordService.Hash(apiKey);
            apiClient.Name = name;
            apiClient.IsActive = true;
            apiClient.RevokedAt = null;
            apiClient.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}
