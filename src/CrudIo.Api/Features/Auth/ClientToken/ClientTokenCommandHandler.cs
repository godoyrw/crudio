using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Data;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Features.Auth.ClientToken;

public class ClientTokenCommandHandler : IRequestHandler<ClientTokenCommand, ClientTokenResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public ClientTokenCommandHandler(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IJwtService jwtService,
        JwtSettings jwtSettings)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings;
    }

    public async Task<ClientTokenResponse> Handle(
        ClientTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) ||
            string.IsNullOrWhiteSpace(request.ClientApiKey))
        {
            throw new UnauthorizedAccessException("Invalid client credentials.");
        }

        var apiClient = await _dbContext.ApiClients
            .FirstOrDefaultAsync(x =>
                x.ClientId == request.ClientId &&
                x.IsActive &&
                x.RevokedAt == null,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid client credentials.");

        var isApiKeyValid = _passwordService.Verify(
            request.ClientApiKey,
            apiClient.ApiKeyHash);

        if (!isApiKeyValid)
            throw new UnauthorizedAccessException("Invalid client credentials.");

        var refreshToken = RefreshTokenService.Generate();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(
            _jwtSettings.ClientRefreshTokenExpirationDays);

        _dbContext.ClientRefreshTokens.Add(new ClientRefreshToken
        {
            Id = Guid.NewGuid(),
            ApiClientId = apiClient.Id,
            TokenHash = RefreshTokenService.Sha256(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateClientToken(
            apiClient.Id,
            apiClient.ClientId);
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
