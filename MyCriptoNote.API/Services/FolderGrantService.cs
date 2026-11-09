using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MyCriptoNote.API.Services;

// A short-lived capability is bound to both the authenticated user and folder.
public class FolderGrantService(IConfiguration configuration)
{
    private const string Audience = "MyCriptoNote.FolderGrant";
    private SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

    public string Issue(Guid userId, Guid folderId, string sessionId)
    {
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"], audience: Audience,
            claims: [new Claim("uid", userId.ToString()), new Claim("folder", folderId.ToString()), new Claim("sid", sessionId)],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool HasGrant(HttpContext context, Guid userId, Guid folderId)
    {
        var value = context.Request.Headers["X-Folder-Grants"].ToString();
        if (value.Length > 16000) return false;
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true, ValidAudience = Audience,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                }, out _);
                if (principal.FindFirst("uid")?.Value == userId.ToString() &&
                    principal.FindFirst("folder")?.Value == folderId.ToString() &&
                    principal.FindFirst("sid")?.Value == context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value &&
                    !string.IsNullOrEmpty(principal.FindFirst("sid")?.Value)) return true;
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException) { }
        }
        return false;
    }
}
