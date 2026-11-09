using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Auth;

public class GoogleLinkRequest
{
    [Required]
    public string Credential { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
