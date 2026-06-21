namespace CrudIo.Api.Data.Entities;

public class ApiClient
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public ICollection<ClientRefreshToken> RefreshTokens { get; set; } = new List<ClientRefreshToken>();
}
