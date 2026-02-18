using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Solution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 1)
        {
            var error = errors[0];

            var statusCode = error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(
                statusCode: statusCode,
                title: error.Code,
                detail: error.Description);
        }

        return ValidationProblem(new ValidationProblemDetails(
            errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description }
            )
        ));
    }
}
