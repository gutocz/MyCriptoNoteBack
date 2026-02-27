using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Notes;

public class MoveToFolderRequest
{
    [Required(ErrorMessage = "O ID da pasta é obrigatório.")]
    public Guid FolderId { get; set; }
}
