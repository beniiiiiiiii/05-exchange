namespace Solution.Core.Models.Requests;

public class UpdateUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("Email")]
    public string Email { get; set; }

    [JsonPropertyName("role")]
    public UserRole Role { get; set; }
}
