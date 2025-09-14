using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tryout.Data;

namespace tryout.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly EcommerceDbContext _db;
    public OrdersController(EcommerceDbContext db) => _db = db;

    public async Task<IActionResult> MyOrders()
    {
        var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var orders = await _db.Orders.Where(o => o.CustomerId == userId).Include(o => o.Items).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> ThankYou(int id)
    {
        var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id);
        return View(order);
    }
}
