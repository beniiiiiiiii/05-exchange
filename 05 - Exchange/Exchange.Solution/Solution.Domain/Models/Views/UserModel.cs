using System;
using System.Collections.Generic;
using System.Text;

namespace Solution.Domain.Models.Views;

public class UserModel
{
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
