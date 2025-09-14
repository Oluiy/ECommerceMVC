// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using tryout.Data;
// using tryout.Models;
// using tryout.Services;
//
// namespace tryout.Controllers;
//
// [Route("account")]
// public class AccountController : Controller
// {
//     private readonly ECommerceDbContext _db;
//     private readonly TokenService _tokenService;
//     private readonly IConfiguration _config;
//
//     public AccountController(ECommerceDbContext db, TokenService tokenService, IConfiguration config)
//     {
//         _db = db; _tokenService = tokenService; _config = config;
//     }
//
//     [HttpGet("register")] public IActionResult Register() => View();
//     [HttpGet("login")] public IActionResult Login() => View();
//
//     [HttpPost("register")]
//     public async Task<IActionResult> Register(Register vm)
//     {
//         if (!ModelState.IsValid) return View(vm);
//         if (await _db.Customers.AnyAsync(c => c.Email == vm.Email))
//         {
//             ModelState.AddModelError("", "Email already exists");
//             return View(vm);
//         }
//
//         Passwordcrypt.CreatePasswordHash(vm.Password, out var hash, out var salt);
//         var customer = new Customer
//         {
//             FirstName = vm.FirstName,
//             LastName = vm.LastName,
//             Email = vm.Email,
//             PasswordHash = hash,
//             PasswordSalt = salt,
//             DateOfBirth = vm.DateOfBirth,
//             PhoneNumber = vm.PhoneNumber
//         };
//
//         _db.Customers.Add(customer);
//         await _db.SaveChangesAsync();
//
//         // create tokens
//         var jwt = _tokenService.CreateJwtToken(customer.Id, customer.Email, minutes: int.Parse(_config["Jwt:ExpiryMinutes"]));
//         var refresh = _tokenService.GenerateRefreshToken();
//         _db.RefreshTokens.Add(new RefreshToken { Token = refresh, Expires = DateTime.UtcNow.AddDays(30), CustomerId = customer.Id });
//         await _db.SaveChangesAsync();
//
//         // set cookie
//         Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])) });
//         Response.Cookies.Append("refresh_token", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(30) });
//
//         return RedirectToAction("Index", "Home");
//     }
//
//     [HttpPost("login")]
//     public async Task<IActionResult> Login(LoginViewModel vm)
//     {
//         if (!ModelState.IsValid) return View(vm);
//
//         var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == vm.Email);
//         if (customer == null || !Passwordcrypt.VerifyPasswordHash(vm.Password, customer.PasswordHash, customer.PasswordSalt))
//         {
//             ModelState.AddModelError("", "Invalid credentials");
//             return View(vm);
//         }
//
//         var jwt = _tokenService.CreateJwtToken(customer.Id, customer.Email, minutes: int.Parse(_config["Jwt:ExpiryMinutes"]));
//         var refresh = _tokenService.GenerateRefreshToken();
//         _db.RefreshTokens.Add(new RefreshToken { Token = refresh, Expires = DateTime.UtcNow.AddDays(30), CustomerId = customer.Id });
//         await _db.SaveChangesAsync();
//
//         Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])) });
//         Response.Cookies.Append("refresh_token", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(30) });
//
//         return RedirectToAction("Index", "Home");
//     }
//
//     [Authorize]
//     [HttpPost("logout")]
//     public async Task<IActionResult> Logout()
//     {
//         // revoke refresh tokens for this user
//         var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
//         var tokens = await _db.RefreshTokens.Where(t => t.CustomerId == userId && !t.IsRevoked).ToListAsync();
//         foreach(var t in tokens) t.IsRevoked = true;
//         await _db.SaveChangesAsync();
//
//         Response.Cookies.Delete("access_token");
//         Response.Cookies.Delete("refresh_token");
//         return RedirectToAction("Index", "Home");
//     }
//
//     // Refresh endpoint
//     [HttpPost("refresh")]
//     public async Task<IActionResult> Refresh()
//     {
//         var refresh = Request.Cookies["refresh_token"];
//         if (string.IsNullOrEmpty(refresh)) return Unauthorized();
//         var stored = await _db.RefreshTokens.Include(t => t.Customer).FirstOrDefaultAsync(t => t.Token == refresh && !t.IsRevoked);
//         if (stored == null || stored.Expires < DateTime.UtcNow) return Unauthorized();
//
//         // rotate
//         stored.IsRevoked = true;
//         var newRefresh = _tokenService.GenerateRefreshToken();
//         _db.RefreshTokens.Add(new RefreshToken { Token = newRefresh, Expires = DateTime.UtcNow.AddDays(30), CustomerId = stored.CustomerId });
//
//         var jwt = _tokenService.CreateJwtToken(stored.CustomerId, stored.Customer!.Email, minutes: int.Parse(_config["Jwt:ExpiryMinutes"]));
//         await _db.SaveChangesAsync();
//
//         Response.Cookies.Append("access_token", jwt, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])) });
//         Response.Cookies.Append("refresh_token", newRefresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(30) });
//
//         return Ok();
//     }
// }
