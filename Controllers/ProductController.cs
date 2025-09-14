using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tryout.Data;

namespace tryout.Controllers;

public class ProductsController : Controller
{
    private readonly EcommerceDbContext _db;
    public ProductsController(EcommerceDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Products.ToListAsync());
    public async Task<IActionResult> Details(int id) => View(await _db.Products.FindAsync(id));
}
