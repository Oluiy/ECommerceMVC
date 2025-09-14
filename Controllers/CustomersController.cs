
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using tryout.Data;
using tryout.Models;
using tryout.Services;
using tryout.ViewModels;


namespace tryout.Controllers;

[Route("account")]
public class CustomersController : Controller
{
    private readonly EcommerceDbContext _db;
    private readonly TokenService _tokenService;
    private readonly RefreshTokenService _refreshService;
    private readonly IConfiguration _config;

    public CustomersController(EcommerceDbContext db, TokenService tokenService, RefreshTokenService refreshService, IConfiguration config)
    {
        _db = db; _tokenService = tokenService; _refreshService = refreshService; _config = config;
    }

    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (await _db.Customers.AnyAsync(c => c.Email == vm.Email))
        {
            ModelState.AddModelError("", "Email already exists.");
            return View(vm);
        }

        Passwordcrypt.CreatePasswordHash(vm.Password, out var hash, out var salt);
        var customer = new Customer
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Email = vm.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            PhoneNumber = vm.PhoneNumber,
            DateOfBirth = vm.DateOfBirth.ToUniversalTime(),
            IsActive = true
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // create tokens
        var jwt = _tokenService.CreateJwtToken(customer.Id, customer.Email, int.Parse(_config["Jwt:ExpiryMinutes"]));
        var refresh = _tokenService.GenerateRefreshToken();
        await _refreshService.SaveRefreshTokenAsync(customer.Id, refresh, DateTime.UtcNow.AddDays(30));

        // set cookies
        Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"]))});
        Response.Cookies.Append("refresh_token", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(7) });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("login")]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == vm.Email);
        if (customer == null || !Passwordcrypt.VerifyPasswordHash(vm.Password, customer.PasswordHash, customer.PasswordSalt))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        var jwt = _tokenService.CreateJwtToken(customer.Id, customer.Email, int.Parse(_config["Jwt:ExpiryMinutes"]));
        var refresh = _tokenService.GenerateRefreshToken();
        await _refreshService.SaveRefreshTokenAsync(customer.Id, refresh, DateTime.UtcNow.AddDays(30));

        Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])) });
        Response.Cookies.Append("refresh_token", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(30) });

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!?? "2");
            await _refreshService.RevokeAllAsync(userId);
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refresh = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refresh)) return Unauthorized();

        var stored = await _refreshService.ValidateAsync(refresh);
        if (stored == null) return Unauthorized();

        // rotate
        var newRefresh = await _refreshService.RotateAsync(stored);
        var jwt = _tokenService.CreateJwtToken(stored.CustomerId, stored.Customer!.Email, int.Parse(_config["Jwt:ExpiryMinutes"]));

        Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])) });
        Response.Cookies.Append("refresh_token", newRefresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(30) });
        return Ok();
    }
}
