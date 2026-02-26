using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Notes;

public class MoveToFolderRequest
{
    [Required(ErrorMessage = "A senha da nota é obrigatória.")]
    public string NotePassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "O ID da pasta é obrigatório.")]
    public Guid FolderId { get; set; }

    [Required(ErrorMessage = "A senha da pasta é obrigatória.")]
    public string FolderPassword { get; set; } = string.Empty;
}
