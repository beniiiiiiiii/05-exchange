namespace Solution.Core.Models.Responses;

public class UserListResponse
{
    [JsonPropertyName("users")]
    public List<UserResponseModel> Users { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
