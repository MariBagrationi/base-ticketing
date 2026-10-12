using TixFlow.Domain.Enums;

namespace TixFlow.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public DateTimeOffset GrantedAt { get; set; }

    public User User { get; set; } = null!;
}
