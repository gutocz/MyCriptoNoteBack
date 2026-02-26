namespace MyCriptoNote.API.DTOs.Folders;

public class FolderListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
