using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tryout.Data;
using tryout.Models;
using tryout.ViewModels;

namespace tryout.Services;

public class RefreshTokenService
{
    private readonly EcommerceDbContext _db;
    public RefreshTokenService(EcommerceDbContext db) => _db = db;

    // Save new token
    public async Task SaveRefreshTokenAsync(int customerId, string token, DateTime expires)
    {
        var rt = new RefreshToken { Token = token, Expires = expires, CustomerId = customerId };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();
    }

    // Validate token, return RefreshToken entity (null if invalid)
    public async Task<RefreshToken?> ValidateAsync(string token)
    {
        var stored = await _db.RefreshTokens.Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);
        if (stored == null) return null;
        if (stored.Expires < DateTime.UtcNow) return null;
        return stored;
    }

    // Rotate: revoke existing token and create new one
    public async Task<string> RotateAsync(RefreshToken existing)
    {
        existing.IsRevoked = true;
        var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var rt = new RefreshToken { Token = newToken, Expires = DateTime.UtcNow.AddDays(30), CustomerId = existing.CustomerId };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();
        return newToken;
    }

    // Revoke all tokens for customer (logout all)
    public async Task RevokeAllAsync(int customerId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.CustomerId == customerId && !t.IsRevoked).ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);
        await _db.SaveChangesAsync();
    }
}
