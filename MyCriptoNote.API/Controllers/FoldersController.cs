using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Data;
using MyCriptoNote.API.DTOs.Folders;
using MyCriptoNote.API.Exceptions;
using MyCriptoNote.API.Extensions;
using MyCriptoNote.API.Models;

namespace MyCriptoNote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _context;

    public FoldersController(AppDbContext context)
    {
        _context = context;
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

        return Ok();
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

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
