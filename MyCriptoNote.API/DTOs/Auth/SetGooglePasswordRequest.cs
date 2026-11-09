using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Auth;

public class SetGooglePasswordRequest
{
    [Required] public string Credential { get; set; } = string.Empty;
    [Required, MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$")]
    public string Password { get; set; } = string.Empty;
}
