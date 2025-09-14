using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace tryout.Services;
public class TokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;
    
    private byte[] GetSigningKeyBytes()
    {
        var keyString = _config["Jwt:Key"] ?? "";
        if (string.IsNullOrWhiteSpace(keyString))
            throw new InvalidOperationException("Jwt:Key is not configured. Set Jwt:Key in appsettings or environment.");

        // try base64 first
        try
        {
            return Convert.FromBase64String(keyString);
        }
        catch (FormatException)
        {
            // not base64 — use UTF8 bytes (safe fallback)
            return Encoding.UTF8.GetBytes(keyString);
        }
    }

    public string CreateJwtToken(int userId, string email, int minutes)
    {
        var keyBytes = GetSigningKeyBytes();
        var cred = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: cred
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}


