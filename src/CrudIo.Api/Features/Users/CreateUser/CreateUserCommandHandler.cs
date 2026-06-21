using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Data;
using CrudIo.Api.Data.Entities;

namespace CrudIo.Api.Features.Users.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public CreateUserCommandHandler(
        AppDbContext dbContext,
        IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await _dbContext.Users
            .AnyAsync(
                x => x.Email == request.Email,
                cancellationToken);

        if (emailExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Email), "Email already in use.")
            });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordService.Hash(request.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateUserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
