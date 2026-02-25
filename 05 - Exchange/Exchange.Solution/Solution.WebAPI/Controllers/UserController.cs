namespace Solution.WebAPI.Controllers;

[Authorize(Roles = "Administrator")]
[Route("Controller")]
public class UserController(IUserManagementService userManagementService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAllUsersAsync()
    {
        var result = await userManagementService.GetAllUsersAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] string id)
    {
        var result = await userManagementService.GetUserByIdAsync(id);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody][Required] CreateUserRequest request)
    {
        var result = await userManagementService.CreateUserAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetUserByIdAsync), new { id = result.Id }, result),
            errors => Problem(errors)
        );
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateUserAsync(
        [FromRoute] string id,
        [FromBody][Required] UpdateUserRequest request)
    {
        var result = await userManagementService.UpdateUserAsync(id, request);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] string id)
    {
        var currentUserId = User.FindFirstValue("uid");
        var result = await userManagementService.DeleteUserAsync(id, currentUserId);
        return result.Match(
            _ => NoContent(),
            errors => Problem(errors)
        );
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromRoute] string id,
        [FromBody][Required] ResetPasswordRequest request)
    {
        var result = await userManagementService.ResetPasswordAsync(id, request);
        return result.Match(
            _ => NoContent(),
            errors => Problem(errors)
        );
    }
}