using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Notes;

public class RemoveFromFolderRequest
{
    [Required(ErrorMessage = "A senha da pasta é obrigatória.")]
    public string FolderPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha da nota é obrigatória.")]
    [MinLength(4, ErrorMessage = "A nova senha deve ter no mínimo 4 caracteres.")]
    public string NewNotePassword { get; set; } = string.Empty;
}
