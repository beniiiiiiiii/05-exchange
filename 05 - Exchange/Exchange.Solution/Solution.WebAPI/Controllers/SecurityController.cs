using Swashbuckle.AspNetCore.Annotations;

namespace Solution.WebAPI.Controllers;

[ApiController]
[ProducesResponseType(statusCode: 400, type: typeof(BadRequestObjectResult))]
public class SecurityController(ISecurityService securityService) : ControllerBase
{
    [HttpPost]
    [Route("api/security/register")]
    [ProducesResponseType(type : typeof(Success), statusCode : 200 )]
    [EndpointDescription("This endpoint will register a new user into the database")]
    public async Task<IActionResult> RegisterAsync([FromBody][Required] RegisterRequestModel model)
    {
        var result = await securityService.RegisterAsync(model);
        return result.Match(
            value => Ok(value),
            errors => errors.ToProblemResult()
            );
    }

    [HttpPost]
    [Route("api/security/login")]
    [ProducesResponseType(type: typeof(TokenResponseModel), statusCode: 200) ]
    [EndpointDescription("This endpoint will log you into a user in the database provided you have the correct email and password")]
    public async Task<IActionResult> LoginAsync([FromBody][Required] LoginRequestModel model)
    {
        var result = await securityService.LoginAsync(model);
        return result.Match(
            value => Ok(value),
            errors => errors.ToProblemResult()
            );
    }
}
