namespace Solution.Domain.Database.Entities;

public class UserEntity : IdentityUser<Guid>
{
    public string FullName { get; set; }

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}
