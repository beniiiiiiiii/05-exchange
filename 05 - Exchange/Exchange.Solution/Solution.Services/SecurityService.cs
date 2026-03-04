namespace Solution.Services;

public class SecurityService(UserManager<UserEntity> userManager, IOptions<JWTSettingsModel> settings) : ISecurityService
{
    public async Task<ErrorOr<Success>> RegisterAsync(RegisterRequestModel model)
    {
        var result = await userManager.CreateAsync(new UserEntity
        {
            Email = model.Email,
            EmailConfirmed = true,
            FullName = $"{model.FirstName} {model.LastName}",
            PhoneNumber = model.PhoneNumber,
            PhoneNumberConfirmed = true,
            UserName = $"{model.FirstName}.{model.LastName}"
        }, model.Password);

        var errors = result.Errors.Select(x => x.Description);
        return result.Succeeded ? Result.Success : Error.Failure(description: string.Join(", ", errors));
    }

    public async Task<ErrorOr<Success>> RegisterAdminAsync(RegisterRequestModel model)
    {
        var user = new UserEntity
        {
            Email = model.Email,
            EmailConfirmed = true,
            FullName = $"{model.FirstName} {model.LastName}",
            PhoneNumber = model.PhoneNumber,
            PhoneNumberConfirmed = true,
            UserName = $"{model.FirstName}.{model.LastName}",
            Role = UserRole.Admin
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Administrator");
        }

        var errors = result.Errors.Select(x => x.Description);
        return result.Succeeded ? Result.Success : Error.Failure(description: string.Join(", ", errors));
    }

    public async Task<ErrorOr<TokenResponseModel>> LoginAsync(LoginRequestModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            return Error.Unauthorized(description: "Invalid credentials");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);

        return isPasswordValid ?
               await GetTokenAsync(user) :
               Error.Unauthorized(description: "Invalid credentials");
    }

    private async Task<TokenResponseModel> GetTokenAsync(UserEntity user)
    {
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(settings.Value.DurationInMinutes);

        var token = new JwtSecurityToken(
            issuer: settings.Value.Issuer,
            audience: settings.Value.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new TokenResponseModel
        {
            Roles = roles,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expiration
        };
    }
}
