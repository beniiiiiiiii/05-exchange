using Solution.Domain.Enums;

namespace Solution.Core.Models.Requests;

public class CreateUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("role")]
    public UserRole Role { get; set; }
}
