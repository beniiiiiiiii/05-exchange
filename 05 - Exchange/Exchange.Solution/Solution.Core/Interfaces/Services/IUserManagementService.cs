

namespace Solution.Core.Interfaces.Services;

public interface IUserManagementService
{
    Task<ErrorOr<UserListResponse>> GetAllUsersAsync();
    Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId);
    Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request);
    Task<ErrorOr<UserResponseModel>> UpdateUSerAsync(string userId, UpdateUserRequest request);
    Task<ErrorOr<Success>> DeleteUserAsync(string userId, string currentUserId);
    Task<ErrorOr<Success>> ResetPasswordAsync(string userId, ResetPasswordRequest request);
}
