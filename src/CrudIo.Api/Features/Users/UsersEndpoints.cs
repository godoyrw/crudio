using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CrudIo.Api.Features.Users.CreateUser;
using CrudIo.Api.Features.Users.GetUser;
using CrudIo.Api.Features.Users.UpdateUser;
using CrudIo.Api.Features.Users.DeleteUser;
using CrudIo.Api.Features.Users.ListUsers;

namespace CrudIo.Api.Features.Users;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // POST: Criar usuário
        group.MapPost("/", async (
            HttpContext context,
            ISender sender,
            [FromBody] CreateUserCommand command) =>
        {
            try
            {
                var result = await sender.Send(command);

                return Results.Created(
                    $"/api/users/{result.Id}",
                    result);
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
            catch (Exception ex)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    ex.GetType().Name,
                    ex.Message);
            }
        })
        .WithName("CreateUser")
        .Produces<CreateUserResponse>(StatusCodes.Status201Created)
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        // GET: Buscar usuário por ID
        group.MapGet("/{id:guid}", async (
            HttpContext context,
            ISender sender,
            Guid id) =>
        {
            try
            {
                var result = await sender.Send(
                    new GetUserQuery(id));

                if (result is null)
                {
                    return ErrorResponse.Create(
                        context,
                        StatusCodes.Status404NotFound,
                        "USER_NOT_FOUND",
                        "User not found.");
                }

                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status404NotFound,
                    "USER_NOT_FOUND",
                    "User not found.");
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
        .WithName("GetUser")
        .Produces<GetUserResponse>(StatusCodes.Status200OK)
        .Produces<ApiError>(StatusCodes.Status404NotFound)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        // PUT: Atualizar usuário
        group.MapPut("/{id:guid}", async (
            HttpContext context,
            ISender sender,
            Guid id,
            [FromBody] UpdateUserCommand command) =>
        {
            if (id != command.Id)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status400BadRequest,
                    "ID_MISMATCH",
                    "Route ID differs from payload ID.");
            }

            try
            {
                var result = await sender.Send(command);

                return Results.Ok(result);
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
            catch (KeyNotFoundException)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status404NotFound,
                    "USER_NOT_FOUND",
                    "User not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status409Conflict,
                    "EMAIL_ALREADY_EXISTS",
                    ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    ex.GetType().Name,
                    ex.Message);
            }
        })
        .WithName("UpdateUser")
        .Produces<UpdateUserResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces<ApiError>(StatusCodes.Status404NotFound)
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        // DELETE: Excluir usuário
        group.MapDelete("/{id:guid}", async (
            HttpContext context,
            ISender sender,
            Guid id) =>
        {
            var result = await sender.Send(
                new DeleteUserCommand(id));

            if (!result)
            {
                return ErrorResponse.Create(
                    context,
                    StatusCodes.Status404NotFound,
                    "USER_NOT_FOUND",
                    "User not found.");
            }

            return Results.Ok(new
            {
                Success = true,
                Message = "User deleted successfully."
            });
        })
        .WithName("DeleteUser")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiError>(StatusCodes.Status404NotFound)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized)
        .Produces<ApiError>(StatusCodes.Status500InternalServerError);
        
        // GET: Listar usuários com paginação
        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 10) =>
        {
            var result = await sender.Send(
                new ListUsersQuery(page, pageSize));

            return Results.Ok(result);
        })
        .WithName("ListUsers")
        .Produces<ListUsersResponse>(StatusCodes.Status200OK)
        .Produces<ApiError>(StatusCodes.Status401Unauthorized);
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
