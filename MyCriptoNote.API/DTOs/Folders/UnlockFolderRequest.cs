using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Folders;

public class UnlockFolderRequest
{
    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;
}
