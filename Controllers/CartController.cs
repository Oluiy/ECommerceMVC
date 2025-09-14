using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using tryout.Data;
using tryout.Dtos;
using tryout.Models;

namespace tryout.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public class CartController : Controller
{
    private readonly EcommerceDbContext _db;
    private const string CART_KEY = "cart_v1";

    public CartController(EcommerceDbContext db) => _db = db;

    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CART_KEY) ?? new List<CartItemDto>();
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int qty = 1)
    {
        var p = await _db.Products.FindAsync(productId);
        if (p == null) return NotFound();

        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CART_KEY) ?? new List<CartItemDto>();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            cart.Add(new CartItemDto { ProductId = productId, ProductName = p.Name, UnitPrice = p.Price, Quantity = qty });
        }
        else
        {
            item.Quantity += qty;
        }

        HttpContext.Session.SetObject(CART_KEY, cart);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CART_KEY) ?? new List<CartItemDto>();
        cart.RemoveAll(i => i.ProductId == productId);
        HttpContext.Session.SetObject(CART_KEY, cart);
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Checkout()
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CART_KEY);
        if (cart == null || cart.Count == 0) return RedirectToAction("Index");

        var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        // create order
        var order = new Order { CustomerId = userId, OrderDate = DateTime.UtcNow, TotalAmount = cart.Sum(i => i.UnitPrice * i.Quantity) };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // to get order.Id

        foreach (var it in cart)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice
            });

            // decrement stock
            var prod = await _db.Products.FindAsync(it.ProductId);
            if (prod != null)
            {
                prod.StockQuantity = Math.Max(0, prod.StockQuantity - it.Quantity);
            }
        }

        await _db.SaveChangesAsync();

        // clear cart
        HttpContext.Session.Remove(CART_KEY);
        return RedirectToAction("ThankYou", "Orders", new { id = order.Id });
    }
}
