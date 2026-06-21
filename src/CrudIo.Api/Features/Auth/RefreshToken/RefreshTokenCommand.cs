using MediatR;
using CrudIo.Api.Features.Auth.ClientToken;

namespace CrudIo.Api.Features.Auth.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken) : IRequest<ClientTokenResponse>;
