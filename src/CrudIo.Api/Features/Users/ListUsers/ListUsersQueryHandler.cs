using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Data;

namespace CrudIo.Api.Features.Users.ListUsers;

public sealed class ListUsersQueryHandler
    : IRequestHandler<ListUsersQuery, ListUsersResponse>
{
    private readonly AppDbContext _dbContext;

    public ListUsersQueryHandler(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ListUsersResponse> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var totalItems = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new UserItemResponse
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ListUsersResponse
        {
            Items = users,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)request.PageSize)
        };
    }
}