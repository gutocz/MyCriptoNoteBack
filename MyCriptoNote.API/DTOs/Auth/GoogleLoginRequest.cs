using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Auth;

public class GoogleLoginRequest
{
    [Required]
    public string Credential { get; set; } = string.Empty;
}
