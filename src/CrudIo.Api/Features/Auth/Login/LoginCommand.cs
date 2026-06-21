using MediatR;

namespace CrudIo.Api.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponse>;

public record LoginResponse(
    string Token,
    int ExpiresIn,
    DateTime ExpiresAt);
