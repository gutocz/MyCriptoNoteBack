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
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace MyCriptoNote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, ITokenService tokenService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
            return BadRequest(new { error = "errors.emailInUse" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AuthProvider = "local",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(ToAuthResponse(user, token));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Unauthorized(new { error = "errors.invalidCredentials" });

        var provider = string.IsNullOrWhiteSpace(user.AuthProvider) ? "local" : user.AuthProvider;
        if (provider != "local" || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(new { error = "errors.invalidCredentials" });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "errors.invalidCredentials" });

        var token = _tokenService.GenerateToken(user);

        return Ok(ToAuthResponse(user, token));
    }

    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return StatusCode(500, new { error = "errors.googleNotConfigured" });

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, BuildGoogleValidation(clientId));

            if (string.IsNullOrWhiteSpace(payload?.Email) || string.IsNullOrWhiteSpace(payload?.Subject))
                return BadRequest(new { error = "errors.googleLoginFailed" });

            if (!payload.EmailVerified)
                return BadRequest(new { error = "errors.googleLoginFailed" });

            var emailNorm = NormalizeEmail(payload.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);
            if (user != null)
            {
                var token = _tokenService.GenerateToken(user);
                return Ok(ToAuthResponse(user, token));
            }

            var emailUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailNorm);
            if (emailUser != null)
                return BadRequest(new { error = "errors.googleLoginFailed" });

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = emailNorm,
                AuthProvider = "google",
                GoogleId = payload.Subject,
                PasswordHash = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var newToken = _tokenService.GenerateToken(newUser);
            return Ok(ToAuthResponse(newUser, newToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google login token validation failed");
            return BadRequest(new { error = "errors.googleLoginFailed" });
        }
    }

    [HttpPost("google/link")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> GoogleLink(GoogleLinkRequest request)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return StatusCode(500, new { error = "errors.googleNotConfigured" });

        var userId = User.GetUserId();
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (currentUser == null)
            throw new NotFoundException("Usuário não encontrado.");

        var alreadyGoogle = NormalizeAuthProvider(currentUser) == "google";
        if (!alreadyGoogle)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "errors.googleLinkPasswordRequired" });

            if (string.IsNullOrEmpty(currentUser.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, currentUser.PasswordHash))
                return BadRequest(new { error = "errors.invalidCredentials" });
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, BuildGoogleValidation(clientId));

            if (string.IsNullOrWhiteSpace(payload?.Subject))
                return BadRequest(new { error = "errors.googleLoginFailed" });

            if (!payload.EmailVerified)
                return BadRequest(new { error = "errors.googleLoginFailed" });

            if (string.IsNullOrWhiteSpace(payload.Email))
                return BadRequest(new { error = "errors.googleLoginFailed" });

            if (alreadyGoogle)
            {
                if (currentUser.GoogleId == payload.Subject)
                {
                    var sameToken = _tokenService.GenerateToken(currentUser);
                    return Ok(ToAuthResponse(currentUser, sameToken));
                }

                return Conflict(new { error = "errors.googleAlreadyLinked" });
            }

            var googleEmail = NormalizeEmail(payload.Email);
            var accountEmail = NormalizeEmail(currentUser.Email);
            if (!string.Equals(googleEmail, accountEmail, StringComparison.Ordinal))
                return BadRequest(new { error = "errors.googleEmailMismatch" });

            var existingGoogleUser = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);
            if (existingGoogleUser != null && existingGoogleUser.Id != currentUser.Id)
                return Conflict(new { error = "errors.googleAccountAlreadyLinked" });

            currentUser.AuthProvider = "google";
            currentUser.GoogleId = payload.Subject;
            currentUser.PasswordHash = null;
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(currentUser);
            return Ok(ToAuthResponse(currentUser, token));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google link token validation failed");
            return BadRequest(new { error = "errors.googleLoginFailed" });
        }
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

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userId = User.GetUserId();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new NotFoundException("Usuário não encontrado.");

        return Ok(new MeResponse
        {
            Email = user.Email,
            AuthProvider = NormalizeAuthProvider(user)
        });
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
            AuthProvider = NormalizeAuthProvider(user),
            GoogleIdMasked = MaskGoogleId(user.GoogleId),
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

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static GoogleJsonWebSignature.ValidationSettings BuildGoogleValidation(string clientId) =>
        new()
        {
            Audience = new[] { clientId },
            IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
            ExpirationTimeClockTolerance = TimeSpan.FromMinutes(2)
        };

    private static string NormalizeAuthProvider(User user) =>
        string.IsNullOrWhiteSpace(user.AuthProvider) ? "local" : user.AuthProvider;

    private static AuthResponse ToAuthResponse(User user, string token) =>
        new()
        {
            Token = token,
            Email = user.Email,
            AuthProvider = NormalizeAuthProvider(user)
        };

    private static string? MaskGoogleId(string? googleId)
    {
        if (string.IsNullOrEmpty(googleId))
            return null;
        if (googleId.Length <= 8)
            return "****";
        return $"{googleId[..4]}…{googleId[^4..]}";
    }
}
