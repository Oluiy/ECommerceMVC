using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using tryout.Data;
using tryout.Services;

namespace tryout
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;

            builder.Services.AddControllersWithViews();

// EF / Postgres
            builder.Services.AddDbContext<EcommerceDbContext>(opts =>
                opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

// Token & refresh services
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddScoped<RefreshTokenService>();

// Authentication (JWT bearer reads token from cookie)
            var key = Convert.FromBase64String(config["Jwt:Key"]);
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var cookie = context.Request.Cookies["access_token"];
                            if (!string.IsNullOrEmpty(cookie)) context.Token = cookie;
                            return Task.CompletedTask;
                        }
                    };
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = config["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = config["Jwt:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapDefaultControllerRoute();
            app.Run();


        }
    }
    
}
