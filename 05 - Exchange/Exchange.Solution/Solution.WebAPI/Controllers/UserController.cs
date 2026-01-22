using Swashbuckle.AspNetCore.Annotations;

namespace Solution.WebAPI.Controllers;

[ApiController]
[ProducesResponseType(statusCode: 400, type: typeof(BadRequestObjectResult))]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Route("api/users")]
    [Authorize]
    [ProducesResponseType(type: typeof(ICollection<UserModel>), statusCode: 200)]
    [EndpointDescription("This endpoint will return all users from the database you hiki hoki hokker")]
    public async Task<IActionResult> GetUsersAsync()
    {
        var result = await userService.GetAllUsers();
        return result.Match(
            value => Ok(value),
            errors => errors.ToProblemResult()
            );
    }
}
