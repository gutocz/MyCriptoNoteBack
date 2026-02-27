namespace MyCriptoNote.API.DTOs.Notes;

public class NoteListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? FolderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
