using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestApi.Data;
using RestApi.Models;
using RestApi.Routes.Arsenal;
using RestApi.Routes.Auth;
using RestApi.Routes.Profile;
using RestApi.Routes.Weapons;
using RestApi.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings__Postgres.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(new JwtTokenService(builder.Configuration));

var jwt = builder.Configuration.GetSection("Jwt");

var encodedKey = jwt["SigningKey"]
    ?? throw new InvalidOperationException("Missing Jwt__SigningKey.");

var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(encodedKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<StarterWeaponCatalog>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/auth");
auth.MapRegister();
auth.MapLogin();

app.MapProfile();
app.MapWeapons();
app.MapArsenal();

app.Run();