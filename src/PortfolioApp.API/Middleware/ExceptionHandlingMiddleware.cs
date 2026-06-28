using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.API.Middleware;

/// <summary>
/// Catches exceptions thrown anywhere downstream and translates them into RFC 7807
/// <c>application/problem+json</c> responses. Domain and validation exceptions map to
/// specific 4xx status codes; anything unexpected becomes a generic 500 so internal
/// details never leak to clients.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException),
            EmailAlreadyInUseException => CreateProblem(StatusCodes.Status409Conflict, "Conflict", exception.Message),
            InvalidCredentialsException => CreateProblem(StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            NotFoundException => CreateProblem(StatusCodes.Status404NotFound, "Not Found", exception.Message),
            _ => CreateUnhandledProblem(exception),
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        Dictionary<string, string[]> errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };

    private ProblemDetails CreateUnhandledProblem(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception processing request.");

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        };
    }
}
