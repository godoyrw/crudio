using MediatR;
using Microsoft.EntityFrameworkCore;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Data;

namespace CrudIo.Api.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IJwtService jwtService,
        JwtSettings jwtSettings)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        var isPasswordValid = _passwordService.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return new LoginResponse(
            Token: token,
            ExpiresIn: _jwtSettings.ExpirationMinutes * 60,
            ExpiresAt: expiresAt);
    }
}
