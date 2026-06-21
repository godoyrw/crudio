using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Cache;
using CrudIo.Api.Data;

namespace CrudIo.Api.Features.Users.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly AppDbContext _context;
    private readonly IRedisCacheService _cache;

    public UpdateUserCommandHandler(AppDbContext context, IRedisCacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar o usuário no banco
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException($"User with ID {request.Id} not found");

        // 2. Verificar se o novo email já existe
        var existingEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != request.Id, cancellationToken);

        if (existingEmail is not null)
            throw new InvalidOperationException("Email already registered by another user");

        // 3. Atualizar os campos
        user.Name = request.Name;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Invalidar o cache
        var cacheKey = CacheKeys.UserById(request.Id);
        await _cache.RemoveAsync(cacheKey);

        // 5. Retornar a resposta
        return new UpdateUserResponse(user.Id, user.Name, user.Email, user.CreatedAt, user.UpdatedAt.Value);
    }
}