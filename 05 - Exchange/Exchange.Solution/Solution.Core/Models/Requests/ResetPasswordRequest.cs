namespace Solution.Core.Models.Requests;

public class ResetPasswordRequest
{
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; }
}
