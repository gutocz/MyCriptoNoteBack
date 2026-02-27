using System.ComponentModel.DataAnnotations;

namespace MyCriptoNote.API.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "A senha deve conter letras maiúsculas, minúsculas, números e caracteres especiais.")]
    public string Password { get; set; } = string.Empty;
}
