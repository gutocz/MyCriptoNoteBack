using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Notes;

public class UnlockNoteRequest
{
    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;
}
