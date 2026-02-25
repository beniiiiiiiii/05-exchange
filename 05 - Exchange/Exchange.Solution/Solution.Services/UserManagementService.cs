
namespace Solution.Services.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<UserEntity> userManager;
    private readonly ILogger<UserManagementService> logger;

    public UserManagementService(
        ApplicationDbContext dbContext,
        UserManager<UserEntity> userManager,
        ILogger<UserManagementService> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.logger = logger;
    }

    public async Task<ErrorOr<UserListResponse>> GetAllUsersAsync()
    {
        var users = await dbContext.Users
            .Include(u => u.Role)
            .ToListAsync();

        return new UserListResponse
        {
            Users = users.Select(MapToResponse).ToList(),
            TotalCount = users.Count
        };
    }

    public async Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId)
    {
        var user = await dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user is null)
            return Errors.User.NotFound;

        return MapToResponse(user);
    }

    public async Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Errors.User.EmailAlreadyExists;

        var user = new UserEntity
        {
            FullName = request.Name,
            Email = request.Email,
            UserName = request.Email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Errors.User.CreationFailed;

        await userManager.AddToRoleAsync(user, request.Role.ToString());

        logger.LogInformation("User {Email} created with role {Role}", request.Email, request.Role);

        return await GetUserByIdAsync(user.Id.ToString());
    }

    public async Task<ErrorOr<UserResponseModel>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        var user = await dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user is null)
            return Errors.User.NotFound;

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null && existingUser.Id.ToString() != userId)
            return Errors.User.EmailAlreadyExists;

        user.FullName = request.Name;
        user.Email = request.Email;
        user.UserName = request.Email;

        await dbContext.SaveChangesAsync();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role.ToString());

        logger.LogInformation("User {UserId} updated", userId);

        return await GetUserByIdAsync(userId);
    }

    public async Task<ErrorOr<Success>> DeleteUserAsync(string userId, string currentUserId)
    {
        if (userId == currentUserId)
            return Errors.User.CannotDeleteSelf;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Errors.User.NotFound;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Errors.User.DeletionFailed;

        logger.LogInformation("User {UserId} deleted", userId);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(string userId, ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Errors.User.NotFound;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return Errors.User.PasswordResetFailed;

        logger.LogInformation("Password reset for user {UserId}", userId);

        return Result.Success;
    }

    private static UserResponseModel MapToResponse(UserEntity entity)
    {
        return new UserResponseModel
        {
            Id = entity.Id.ToString(),
            Name = entity.FullName,
            Email = entity.Email,
            Roles = entity.Role.ToString()
        };
    }
}