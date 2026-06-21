using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using StackExchange.Redis;
using MediatR;

using CrudIo.Api.Data;
using CrudIo.Api.Cache;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Features.Users;
using CrudIo.Api.Features.Auth;
using CrudIo.Api.Common.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "appdb";
var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "appuser";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "appuser";

// Configurar o DbContext do PostgreSQL
var postgresConnection = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

// Configurar o DbContext do PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Redis
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? "localhost:6379,password=appuser";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();


builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// JWT
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-secret-key-change-in-production-32-chars-minimum-1234567890";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CrudIo.Api";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CrudIo.Client";
var jwtExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var exp) ? exp : 60;
var clientAccessTokenExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var clientExp) ? clientExp : 15;
var clientRefreshTokenExpirationDays = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS"), out var refreshExp) ? refreshExp : 30;

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationMinutes = jwtExpirationMinutes,
    ClientAccessTokenExpirationMinutes = clientAccessTokenExpirationMinutes,
    ClientRefreshTokenExpirationDays = clientRefreshTokenExpirationDays
};

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // Disponível em /openapi/{documentName}.json
    // app.UseSwaggerUI(options =>     // UI interativa (opcional, mas recomendada)
    // {
    //     options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    // });
}

// Redirecionar HTTP para HTTPS
app.UseHttpsRedirection();

// Autenticação e Autorização
app.UseAuthentication();
app.UseAuthorization();

// Mapear endpoints
app.MapAuthEndpoints();
app.MapUsersEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await ApiClientSeeder.SeedFromEnvironmentAsync(scope.ServiceProvider);
}

app.Run();
