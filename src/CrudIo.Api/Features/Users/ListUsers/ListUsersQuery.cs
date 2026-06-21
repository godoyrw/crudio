using MediatR;

namespace CrudIo.Api.Features.Users.ListUsers;

public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 10) : IRequest<ListUsersResponse>;