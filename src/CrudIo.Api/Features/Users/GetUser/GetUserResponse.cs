namespace CrudIo.Api.Features.Users.GetUser;

public record GetUserResponse(Guid Id, string Name, string Email, DateTime CreatedAt, DateTime? UpdatedAt);