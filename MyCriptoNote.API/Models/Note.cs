namespace MyCriptoNote.API.Models;

public class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EncryptedContent { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string IV { get; set; } = string.Empty;
    public string? AuthTag { get; set; }
    public Guid? FolderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Folder? Folder { get; set; }
}
