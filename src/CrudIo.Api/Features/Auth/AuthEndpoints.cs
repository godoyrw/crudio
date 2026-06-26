using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CrudIo.Api.Features.Auth.ClientToken;
using CrudIo.Api.Features.Auth.Login;
using CrudIo.Api.Features.Auth.RefreshToken;

namespace CrudIo.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/client-token", async (
            HttpContext context,
            ISender sender,
            [FromHeader(Name = "client-id")] string clientId,
            [FromHeader(Name = "client-api-key")] string clientApiKey) =>
        {
            try
            {
                var result = await sender.Send(
                    new ClientTokenCommand(clientId, clientApiKey));

                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "INVALID_CLIENT_CREDENTIALS",
                    "Invalid client credentials.");
            }
            catch (Exception)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred.");
            }
        })
        .WithName("ClientToken")
        .Produces<ClientTokenResponse>(StatusCodes.Status200OK)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapPost("/refresh-token", async (
            HttpContext context,
            ISender sender,
            [FromBody] RefreshTokenCommand command) =>
        {
            try
            {
                var result = await sender.Send(command);

                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "INVALID_REFRESH_TOKEN",
                    "Invalid refresh token.");
            }
            catch (Exception)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred.");
            }
        })
        .WithName("RefreshToken")
        .Produces<ClientTokenResponse>(StatusCodes.Status200OK)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        // POST: Login
        group.MapPost("/login", async (
            HttpContext context,
            ISender sender,
            [FromBody] LoginCommand command) =>
        {
            try
            {
                var result = await sender.Send(command);

                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "INVALID_CREDENTIALS",
                    ex.Message);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(
                    ex.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToArray()));
            }
            catch (Exception)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred.");
            }
        })
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem()
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);
    }
}

public sealed class ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public string? TraceId { get; init; }
}

public static class ErrorResponse
{
    public static IResult Create(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        return Results.Json(
            new ApiError
            {
                Code = code,
                Message = message,
                StatusCode = statusCode,
                TraceId = context.TraceIdentifier
            },
            statusCode: statusCode);
    }
}
