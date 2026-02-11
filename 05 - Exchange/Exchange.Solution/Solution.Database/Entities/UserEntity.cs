namespace Solution.Database.Entities;

public class UserEntity : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public UserRole Role { get; set; } = UserRole.User;
}
