using Microsoft.AspNetCore.Mvc;
using TaskHub.Services;

namespace TaskHub.Controllers.Api;

[IgnoreAntiforgeryToken]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleFailure(ServiceResult result)
    {
        return result.ErrorType switch
        {
            ServiceErrorType.Validation => BadRequest(new ValidationProblemDetails
            {
                Title = result.ErrorMessage ?? "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Errors = result.ValidationErrors != null
        ? new Dictionary<string, string[]>(result.ValidationErrors)
        : new Dictionary<string, string[]>
        {
            ["error"] = new[] { result.ErrorMessage ?? "Validation failed." }
        }
            }),

            ServiceErrorType.NotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                result.ErrorMessage ?? "Resource not found."
            )),

            ServiceErrorType.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, result.ErrorMessage ?? "Forbidden.")
            ),

            ServiceErrorType.Conflict => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                result.ErrorMessage ?? "Conflict."
            )),

            ServiceErrorType.Unauthorized => Unauthorized(CreateProblem(
                StatusCodes.Status401Unauthorized,
                result.ErrorMessage ?? "Unauthorized."
            )),

            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                CreateProblem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            )
        };
    }

    protected string? CurrentUserId => User.GetUserId();

    private static ProblemDetails CreateProblem(int statusCode, string title)
        => new()
        {
            Status = statusCode,
            Title = title
        };
}
