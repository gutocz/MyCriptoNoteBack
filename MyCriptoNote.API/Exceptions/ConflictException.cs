namespace MyCriptoNote.API.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message = "Conflito com recurso existente.")
        : base(message, 409) { }
}
