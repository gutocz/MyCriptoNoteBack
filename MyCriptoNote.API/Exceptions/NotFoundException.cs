namespace MyCriptoNote.API.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message = "Recurso não encontrado.")
        : base(message, 404) { }
}
