using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Folders;

public class CreateFolderRequest
{
    [Required(ErrorMessage = "O nome da pasta é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome da pasta deve ter no máximo 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha da pasta é obrigatória.")]
    [MinLength(4, ErrorMessage = "A senha deve ter no mínimo 4 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
