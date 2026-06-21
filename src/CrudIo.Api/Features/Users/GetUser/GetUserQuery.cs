using MediatR;

namespace CrudIo.Api.Features.Users.GetUser;

public record GetUserQuery(Guid Id) : IRequest<GetUserResponse?>;