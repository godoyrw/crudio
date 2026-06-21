using MediatR;

namespace CrudIo.Api.Features.Users.UpdateUser;

public record UpdateUserCommand(Guid Id, string Name, string Email) : IRequest<UpdateUserResponse>;