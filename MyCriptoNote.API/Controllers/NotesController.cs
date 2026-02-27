using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Data;
using MyCriptoNote.API.DTOs.Notes;
using MyCriptoNote.API.Exceptions;
using MyCriptoNote.API.Extensions;
using MyCriptoNote.API.Models;
using MyCriptoNote.API.Services;

namespace MyCriptoNote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICryptoService _cryptoService;

    public NotesController(AppDbContext context, ICryptoService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NoteListItem>>> GetAll()
    {
        var userId = User.GetUserId();

        var notes = await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteListItem
            {
                Id = n.Id,
                Title = n.Title,
                FolderId = n.FolderId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    [HttpPost]
    public async Task<ActionResult<NoteListItem>> Create(CreateNoteRequest request)
    {
        var userId = User.GetUserId();

        if (request.FolderId.HasValue)
        {
            var folder = await _context.Folders.FirstOrDefaultAsync(
                f => f.Id == request.FolderId.Value && f.UserId == userId);
            if (folder == null)
                throw new NotFoundException("Pasta não encontrada.");
        }

        var result = _cryptoService.Encrypt(request.Content, request.Password);

        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            EncryptedContent = result.EncryptedContent,
            Salt = result.Salt,
            IV = result.IV,
            AuthTag = result.AuthTag,
            FolderId = request.FolderId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new NoteListItem
        {
            Id = note.Id,
            Title = note.Title,
            FolderId = note.FolderId,
            CreatedAt = note.CreatedAt
        });
    }

    [HttpPost("{id}/unlock")]
    public async Task<ActionResult<UnlockedNote>> Unlock(Guid id, UnlockNoteRequest request)
    {
        var userId = User.GetUserId();
        var note = await GetNoteWithOwnershipCheck(id, userId);

        var content = _cryptoService.Decrypt(note.EncryptedContent, request.Password, note.Salt, note.IV, note.AuthTag);

        return Ok(new UnlockedNote
        {
            Id = note.Id,
            Title = note.Title,
            Content = content
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateNoteRequest request)
    {
        var userId = User.GetUserId();
        var note = await GetNoteWithOwnershipCheck(id, userId);

        var currentContent = _cryptoService.Decrypt(note.EncryptedContent, request.Password, note.Salt, note.IV, note.AuthTag);

        var newTitle = request.Title ?? note.Title;
        var newContent = request.Content ?? currentContent;

        var result = _cryptoService.Encrypt(newContent, request.Password, note.Salt);

        note.Title = newTitle;
        note.EncryptedContent = result.EncryptedContent;
        note.IV = result.IV;
        note.AuthTag = result.AuthTag;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var note = await GetNoteWithOwnershipCheck(id, userId);

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/move-to-folder")]
    public async Task<IActionResult> MoveToFolder(Guid id, MoveToFolderRequest request)
    {
        var userId = User.GetUserId();
        var note = await GetNoteWithOwnershipCheck(id, userId);

        var folder = await _context.Folders.FirstOrDefaultAsync(
            f => f.Id == request.FolderId && f.UserId == userId);
        if (folder == null)
            throw new NotFoundException("Pasta não encontrada.");

        note.FolderId = folder.Id;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/remove-from-folder")]
    public async Task<IActionResult> RemoveFromFolder(Guid id)
    {
        var userId = User.GetUserId();
        var note = await GetNoteWithOwnershipCheck(id, userId);

        if (!note.FolderId.HasValue)
            throw new AppException("A nota não está em nenhuma pasta.");

        note.FolderId = null;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Note> GetNoteWithOwnershipCheck(Guid noteId, Guid userId)
    {
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId);
        if (note == null)
            throw new NotFoundException("Nota não encontrada.");
        if (note.UserId != userId)
            throw new ForbiddenException("Você não tem permissão para acessar esta nota.");
        return note;
    }
}
