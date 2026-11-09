namespace MyCriptoNote.API.Models;

public class AuthSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public User User { get; set; } = null!;
}
