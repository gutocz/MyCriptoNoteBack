using MyCriptoNote.API.Models;

namespace MyCriptoNote.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
