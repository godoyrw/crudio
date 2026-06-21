namespace CrudIo.Api.Data.Entities;

public class ClientRefreshToken
{
    public Guid Id { get; set; }
    public Guid ApiClientId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public ApiClient ApiClient { get; set; } = null!;
}
