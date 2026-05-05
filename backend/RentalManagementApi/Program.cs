using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Data;
using RentalManagementApi.Middleware;
using RentalManagementApi.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// ------------------------------------------------------------
// Options
// ------------------------------------------------------------
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.Section));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.Section));
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection(SendGridOptions.Section));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.Section));

// ------------------------------------------------------------
// Database
// ------------------------------------------------------------
var rawConnection = builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DATABASE_URL is not configured.");

var connectionString = ParseConnectionString(rawConnection);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ------------------------------------------------------------
// Authentication — Supabase JWT (HS256)
// ------------------------------------------------------------
var jwtSecret = builder.Configuration["SUPABASE_JWT_SECRET"]
    ?? builder.Configuration["Supabase:JwtSecret"]
    ?? throw new InvalidOperationException("SUPABASE_JWT_SECRET is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["SUPABASE_URL"]
                          ?? builder.Configuration["Supabase:Url"],
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// ------------------------------------------------------------
// Authorization policies
// ------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LandlordPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("user_role", "Landlord"));

    options.AddPolicy("TenantPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("user_role", "Tenant"));
});

// ------------------------------------------------------------
// CORS
// ------------------------------------------------------------
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ------------------------------------------------------------
// Services
// ------------------------------------------------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();

// ------------------------------------------------------------
// Controllers + Swagger
// ------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------------------------------------------------
// Port (Railway injects PORT)
// ------------------------------------------------------------
var port = builder.Configuration["PORT"] ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

var app = builder.Build();

// ------------------------------------------------------------
// Middleware pipeline
// ------------------------------------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ParseConnectionString(string cs)
{
    if (!cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return cs;

    var uri = new Uri(cs);
    var parts = uri.UserInfo.Split(':');
    var user = parts[0];
    var pass = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
    var db = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port > 0 ? uri.Port : 5432;

    return $"Host={uri.Host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
}
