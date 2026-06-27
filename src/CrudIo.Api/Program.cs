using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using StackExchange.Redis;
using MediatR;
using Microsoft.Extensions.Options;

using CrudIo.Api.Data;
using CrudIo.Api.Cache;
using CrudIo.Api.Common.Security;
using CrudIo.Api.Features.Users;
using CrudIo.Api.Features.Auth;
using CrudIo.Api.Common.Behaviors;
using CrudIo.Api.Logging;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("🚀 Starting CrudIo API...");

// ----------------------------
// PostgreSQL
// ----------------------------
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "appdb";
var user = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "appuser";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "appuser";

var postgresConnection =
    $"Host={host};Port={port};Database={db};Username={user};Password={password}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

Console.WriteLine($"📦 Postgres configured: {host}:{port}/{db}");

// ----------------------------
// Redis (fail-safe)
// ----------------------------
var redisConnectionString =
    builder.Configuration.GetConnectionString("RedisConnection")
    ?? "localhost:6379,password=appuser";

try
{
    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

    Console.WriteLine("🟢 Redis connected");
}
catch (Exception ex)
{
    Console.WriteLine($"🟡 Redis unavailable, continuing without cache: {ex.Message}");
}

// ----------------------------
// MongoDB Logger (fail-safe)
// ----------------------------
try
{
    var mongoHost = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "mongodb";
    var mongoPort = Environment.GetEnvironmentVariable("MONGO_PORT") ?? "27017";
    var mongoUser = Environment.GetEnvironmentVariable("MONGO_ROOT_USERNAME") ?? "appuser";
    var mongoPassword = Environment.GetEnvironmentVariable("MONGO_ROOT_PASSWORD") ?? "appuser";

    var mongoConnectionString =
        $"mongodb://{mongoUser}:{mongoPassword}@{mongoHost}:{mongoPort}";

    var mongoDatabaseName =
        Environment.GetEnvironmentVariable("MONGO_DATABASE")
        ?? "appdb_logs";

    builder.Services.Configure<MongoLoggerSettings>(options =>
    {
        options.ConnectionString = mongoConnectionString;
        options.DatabaseName = mongoDatabaseName;
    });

    builder.Services.AddSingleton<IApiLogger, MongoLogger>();

    Console.WriteLine("🟢 MongoDB logger configured");
}
catch (Exception ex)
{
    Console.WriteLine($"🟡 MongoDB unavailable, continuing without request logging: {ex.Message}");
}

// ----------------------------
// MediatR + Validation
// ----------------------------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ----------------------------
// JWT
// ----------------------------
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "your-secret-key-change-in-production-32-chars-minimum-1234567890";

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "CrudIo.Api";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "CrudIo.Client";

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var exp) ? exp : 60,
    ClientAccessTokenExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var acc) ? acc : 15,
    ClientRefreshTokenExpirationDays = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS"), out var refd) ? refd : 30
};

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// ----------------------------
// OpenAPI
// ----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ----------------------------
// Dev-only OpenAPI
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ----------------------------
// Middleware
// ----------------------------

// Middleware de logging de requisições (deve ser adicionado cedo no pipeline)
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;

    // Registrar callback para executar após a resposta ser enviada
    if (HttpMethods.IsGet(context.Request.Method) ||
        HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method) ||
        HttpMethods.IsHead(context.Request.Method) ||
        HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.OnCompleted(() =>
        {
            return Task.Run(async () =>
            {
                try
                {
                    var endTime = DateTime.UtcNow;
                    var elapsedMilliseconds = (long)(endTime - startTime).TotalMilliseconds;

                    var logger = context.RequestServices.GetService<IApiLogger>();
                    if (logger != null)
                    {
                        try
                        {
                            var requestHeaders = new Dictionary<string, string[]>();
                            foreach (var header in context.Request.Headers)
                            {
                                requestHeaders[header.Key] = header.Value.ToArray();
                            }

                            var logEntry = new ApiLogEntry
                            {
                                Timestamp = startTime,
                                HttpMethod = context.Request.Method,
                                Path = context.Request.Path.ToString(),
                                QueryString = context.Request.QueryString.ToString(),
                                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                                UserAgent = context.Request.Headers.UserAgent.ToString() ?? string.Empty,
                                StatusCode = context.Response.StatusCode,
                                RequestHeaders = requestHeaders,
                                ElapsedMilliseconds = elapsedMilliseconds
                            };

                            // Tentar extrair o user ID do JWT se presente
                            if (context.User?.Identity?.IsAuthenticated == true)
                            {
                                var userIdClaim = context.User?.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "user_id");
                                if (userIdClaim != null)
                                {
                                    logEntry.UserId = userIdClaim.Value;
                                }

                                // Tentar extrair client_id se for um token de cliente
                                var clientIdClaim = context.User?.Claims.FirstOrDefault(c => c.Type == "client_id");
                                if (clientIdClaim != null)
                                {
                                    logEntry.ClientId = clientIdClaim.Value;
                                }
                            }

                            // Log de forma assíncrona sem bloquear
                            _ = logger.LogRequestAsync(logEntry);
                        }
                        catch (Exception ex)
                        {
                            // Em caso de erro no logging, não quebrar nada - apenas log no console
                            System.Console.WriteLine($"⚠️ Logging error: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Tratar qualquer outra exceção inesperada no callback
                    System.Console.WriteLine($"⚠️ Unexpected error in logging callback: {ex.Message}");
                }
            });
        });
    }

    // Continuar o pipeline normalmente
    await next();
});
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Middleware global para 404
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            code = "NOT_FOUND",
            message = "The requested endpoint does not exist."
        });
    }
});

// ----------------------------
// DB migration SAFE (não quebra startup)
// ----------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("🧩 Running migrations...");

        await dbContext.Database.MigrateAsync();

        Console.WriteLine("🟢 Migrations applied");

        await ApiClientSeeder.SeedFromEnvironmentAsync(scope.ServiceProvider);

        Console.WriteLine("🌱 Seeder executed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔴 Migration/Seed failed but API will continue: {ex.Message}");
    }
}

// ----------------------------
// Endpoints
// ----------------------------
app.MapGet("/health", () => Results.Ok(
        new { status = "healthy" }
    ));

var ApiV1 = app.MapGroup("/api/v1");
ApiV1.MapAuthEndpoints();
ApiV1.MapUsersEndpoints();


app.Run();

Console.WriteLine("🟢 API is running");