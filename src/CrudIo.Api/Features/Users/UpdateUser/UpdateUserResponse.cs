namespace CrudIo.Api.Features.Users.UpdateUser;

public record UpdateUserResponse(Guid Id, string Name, string Email, DateTime CreatedAt, DateTime UpdatedAt);