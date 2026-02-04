namespace Solution.Domain.Models.Requests.Security;

public class RegisterRequestModel
{
    [Required]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [Required]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; }

    [Required]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; }

    [Required]
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; }
}
