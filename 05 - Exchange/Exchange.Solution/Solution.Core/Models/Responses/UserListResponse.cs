namespace Solution.Core.Models.Responses;

public class UserListResponse
{
    [JsonPropertyName("users")]
    public List<UserResponesModel> Users { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
