namespace CrudIo.Api.Common.Security;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int ClientAccessTokenExpirationMinutes { get; set; } = 15;
    public int ClientRefreshTokenExpirationDays { get; set; } = 30;
}
