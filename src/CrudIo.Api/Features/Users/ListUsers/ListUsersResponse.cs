namespace CrudIo.Api.Features.Users.ListUsers;

public sealed class ListUsersResponse
{
    public IEnumerable<UserItemResponse> Items { get; init; }
        = Enumerable.Empty<UserItemResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}

public sealed class UserItemResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}