using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Data;
using CrudIo.Api.Data.Entities;
using CrudIo.Api.Features.Auth.ClientToken;

namespace CrudIo.Api.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ClientTokenResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(
        AppDbContext dbContext,
        IJwtService jwtService,
        JwtSettings jwtSettings)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings;
    }

    public async Task<ClientTokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var tokenHash = RefreshTokenService.Sha256(request.RefreshToken);

        var storedToken = await _dbContext.ClientRefreshTokens
            .Include(x => x.ApiClient)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.RevokedAt is not null ||
            storedToken.UsedAt is not null ||
            storedToken.ExpiresAt <= DateTime.UtcNow ||
            !storedToken.ApiClient.IsActive ||
            storedToken.ApiClient.RevokedAt is not null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var refreshToken = RefreshTokenService.Generate();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(
            _jwtSettings.ClientRefreshTokenExpirationDays);

        var newStoredToken = new ClientRefreshToken
        {
            Id = Guid.NewGuid(),
            ApiClientId = storedToken.ApiClientId,
            TokenHash = RefreshTokenService.Sha256(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt
        };

        storedToken.UsedAt = DateTime.UtcNow;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenId = newStoredToken.Id;

        _dbContext.ClientRefreshTokens.Add(newStoredToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateClientToken(
            storedToken.ApiClient.Id,
            storedToken.ApiClient.ClientId);
        var expiresAt = DateTime.UtcNow.AddMinutes(
            _jwtSettings.ClientAccessTokenExpirationMinutes);

        return new ClientTokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            TokenType: "Bearer",
            ExpiresIn: _jwtSettings.ClientAccessTokenExpirationMinutes * 60,
            ExpiresAt: expiresAt,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
