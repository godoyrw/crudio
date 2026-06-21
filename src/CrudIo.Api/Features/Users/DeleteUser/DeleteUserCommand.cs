using MediatR;

namespace CrudIo.Api.Features.Users.DeleteUser;

public record DeleteUserCommand(Guid Id) : IRequest<bool>;