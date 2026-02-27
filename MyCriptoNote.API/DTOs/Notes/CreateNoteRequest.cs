using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Notes;

public class CreateNoteRequest
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "O conteúdo é obrigatório.")]
    [MaxLength(1_048_576, ErrorMessage = "O conteúdo da nota excede o tamanho máximo permitido (1 MB).")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(4, ErrorMessage = "A senha deve ter no mínimo 4 caracteres.")]
    public string Password { get; set; } = string.Empty;

    public Guid? FolderId { get; set; }
}
