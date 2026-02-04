namespace Solution.Domain.Models.Requests.Security;

public class LoginRequestModel
{
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; }
}
