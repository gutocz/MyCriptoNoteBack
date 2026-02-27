namespace MyCriptoNote.API.Exceptions;

public class InvalidPasswordException : AppException
{
    public InvalidPasswordException(string message = "Senha inválida. Não foi possível descriptografar a nota.")
        : base(message, 400) { }
}
