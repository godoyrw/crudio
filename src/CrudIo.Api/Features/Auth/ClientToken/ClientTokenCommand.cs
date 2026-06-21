using MediatR;

namespace CrudIo.Api.Features.Auth.ClientToken;

public record ClientTokenCommand(
    string ClientId,
    string ClientApiKey) : IRequest<ClientTokenResponse>;

public record ClientTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt);
