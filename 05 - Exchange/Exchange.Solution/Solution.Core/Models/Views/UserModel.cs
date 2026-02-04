namespace Solution.Core.Models.Views;

[Table("User")]
public class UserModel
{
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
