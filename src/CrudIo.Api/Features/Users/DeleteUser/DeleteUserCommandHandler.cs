using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Cache;
using CrudIo.Api.Data;

namespace CrudIo.Api.Features.Users.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IRedisCacheService _cache;

    public DeleteUserCommandHandler(AppDbContext context, IRedisCacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        var cacheKey = CacheKeys.UserById(request.Id);
        await _cache.RemoveAsync(cacheKey);

        return true;
    }
}