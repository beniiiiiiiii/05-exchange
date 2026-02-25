namespace Solution.Core.Models.Responses;

public class UserResponseModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [Required]
    [JsonPropertyName("role")]
    public string Roles { get; set; }
}
