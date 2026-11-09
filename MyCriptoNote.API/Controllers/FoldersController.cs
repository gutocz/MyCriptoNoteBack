using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Data;
using MyCriptoNote.API.DTOs.Folders;
using MyCriptoNote.API.Exceptions;
using MyCriptoNote.API.Extensions;
using MyCriptoNote.API.Models;
using MyCriptoNote.API.Services;
using System.IdentityModel.Tokens.Jwt;

namespace MyCriptoNote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FolderGrantService _grants;

    public FoldersController(AppDbContext context, FolderGrantService grants)
    {
        _context = context;
        _grants = grants;
    }

    [HttpGet]
    public async Task<ActionResult<List<FolderListItem>>> GetAll()
    {
        var userId = User.GetUserId();

        var folders = await _context.Folders
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FolderListItem
            {
                Id = f.Id,
                Name = f.Name,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(folders);
    }

    [HttpPost]
    public async Task<ActionResult<FolderListItem>> Create(CreateFolderRequest request)
    {
        var userId = User.GetUserId();

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new FolderListItem
        {
            Id = folder.Id,
            Name = folder.Name,
            CreatedAt = folder.CreatedAt
        });
    }

    [HttpPost("{id}/unlock")]
    [EnableRateLimiting("unlock")]
    public async Task<IActionResult> Unlock(Guid id, UnlockFolderRequest request)
    {
        var userId = User.GetUserId();

        var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id);
        if (folder == null)
            throw new NotFoundException("Pasta não encontrada.");
        if (folder.UserId != userId)
            throw new ForbiddenException("Você não tem permissão para acessar esta pasta.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, folder.PasswordHash))
            return Unauthorized(new { error = "Senha incorreta." });

        var sessionId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(sessionId)) return Unauthorized();
        return Ok(new { grant = _grants.Issue(userId, folder.Id, sessionId) });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();

        var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id);
        if (folder == null)
            throw new NotFoundException("Pasta não encontrada.");
        if (folder.UserId != userId)
            throw new ForbiddenException("Você não tem permissão para acessar esta pasta.");

        if (!_grants.HasGrant(HttpContext, userId, folder.Id))
            return StatusCode(403, new { error = "Desbloqueie a pasta novamente." });

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
