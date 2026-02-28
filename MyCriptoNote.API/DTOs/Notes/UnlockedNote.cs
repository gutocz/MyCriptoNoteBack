namespace MyCriptoNote.API.DTOs.Notes;

public class UnlockedNote
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
