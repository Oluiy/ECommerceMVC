using Xunit;
using Microsoft.EntityFrameworkCore;
using tryout.Data;
using tryout.Models;
using tryout.Services;

namespace tryout.Tests;


public class RefreshTokenServiceTests
{
    private EcommerceDbContext CreateDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<EcommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EcommerceDbContext(opts);
    }

    [Fact]
    public async Task RotateAsync_revokes_old_and_creates_new()
    {
        var db = CreateDb("rt_rotate");
        var customer = new Customer { FirstName="A", LastName="B", Email="a@b.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var old = new RefreshToken { Token = "old", CustomerId = customer.Id, Expires = DateTime.UtcNow.AddDays(1) };
        db.RefreshTokens.Add(old);
        await db.SaveChangesAsync();

        var svc = new RefreshTokenService(db);
        var newToken = await svc.RotateAsync(old);

        Assert.NotNull(newToken);
        var storedOld = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token=="old");
        Assert.True(storedOld!.IsRevoked);
        var storedNew = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token==newToken);
        Assert.NotNull(storedNew);
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_expired()
    {
        var db = CreateDb("rt_expired");
        var customer = new Customer { FirstName="A", LastName="B", Email="c@d.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var rt = new RefreshToken { Token="tok", CustomerId = customer.Id, Expires = DateTime.UtcNow.AddDays(-1) };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        var svc = new RefreshTokenService(db);
        var res = await svc.ValidateAsync("tok");
        Assert.Null(res);
    }
}
