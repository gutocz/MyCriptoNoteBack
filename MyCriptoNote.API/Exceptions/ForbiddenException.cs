namespace MyCriptoNote.API.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Você não tem permissão para acessar este recurso.")
        : base(message, 403) { }
}
