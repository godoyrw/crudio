using MediatR;

namespace CrudIo.Api.Features.Users.CreateUser;

public sealed class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
