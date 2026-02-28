using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Data;
using MyCriptoNote.API.DTOs.Auth;
using MyCriptoNote.API.Exceptions;
using MyCriptoNote.API.Extensions;
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
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return BadRequest(new { error = "Não foi possível completar o registro." });

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
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "E-mail ou senha inválidos." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse { Token = token, Email = user.Email });
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new NotFoundException("Usuário não encontrado.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("my-data")]
    [Authorize]
    public async Task<IActionResult> ExportMyData()
    {
        var userId = User.GetUserId();
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Notes)
            .Include(u => u.Folders)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new NotFoundException("Usuário não encontrado.");

        var data = new
        {
            user.Email,
            user.CreatedAt,
            Folders = user.Folders.Select(f => new
            {
                f.Id,
                f.Name,
                f.CreatedAt
            }),
            Notes = user.Notes.Select(n => new
            {
                n.Id,
                n.Title,
                n.FolderId,
                n.CreatedAt
            })
        };

        return Ok(data);
    }
}
