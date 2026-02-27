using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Data;
using MyCriptoNote.API.DTOs.Auth;
using MyCriptoNote.API.Exceptions;
using MyCriptoNote.API.Models;
using MyCriptoNote.API.Services;

namespace MyCriptoNote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            throw new ConflictException("Este e-mail já está em uso.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse { Token = token, Email = user.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "E-mail ou senha inválidos." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse { Token = token, Email = user.Email });
    }
}
