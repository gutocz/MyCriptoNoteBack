namespace MyCriptoNote.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
