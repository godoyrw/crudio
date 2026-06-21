using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Cache;
using CrudIo.Api.Data;

namespace CrudIo.Api.Features.Users.GetUser;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, GetUserResponse?>
{
    private readonly AppDbContext _context;
    private readonly IRedisCacheService _cache;

    public GetUserQueryHandler(AppDbContext context, IRedisCacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<GetUserResponse?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UserById(request.Id);

        // 1. Tentar obter do cache
        var cached = await _cache.GetAsync<GetUserResponse>(cacheKey);
        if (cached is not null)
            return cached;

        // 2. Buscar no banco
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            return null;

        var response = new GetUserResponse(user.Id, user.Name, user.Email, user.CreatedAt, user.UpdatedAt);

        // 3. Armazenar no cache (expira em 10 minutos)
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

        return response;
    }
}