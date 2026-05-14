using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Data;
using RentalManagementApi.Middleware;
using RentalManagementApi.Options;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(k =>
{
    k.AddServerHeader = false;
    k.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB global limit
});

// Load .env file for local development (walk up from working directory to find it)
var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
while (dir != null)
{
    var envFile = Path.Combine(dir.FullName, ".env");
    if (File.Exists(envFile))
    {
        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;
            var key = trimmed[..idx].Trim();
            var val = trimmed[(idx + 1)..].Trim();
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, val);
        }
        break;
    }
    dir = dir.Parent;
}

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
// Authentication — Supabase JWT (ES256 via JWKS)
// Newer Supabase projects sign tokens with ES256 (asymmetric).
// We fetch signing keys from Supabase's JWKS endpoint at startup.
// ------------------------------------------------------------
var supabaseUrl = builder.Configuration["SUPABASE_URL"]
    ?? builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("SUPABASE_URL is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // Keep JWT claim names as-is (don't map sub → ClaimTypes.NameIdentifier, etc.)
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
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
// Rate Limiting
// ------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    // 5 attempts per minute per IP on auth endpoints (register, accept-invite)
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Too many requests. Please try again later.",
                code = "RATE_LIMIT_EXCEEDED"
            }),
            token);
    };
});

// ------------------------------------------------------------
// Services
// ------------------------------------------------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<LeaseService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ProvisionService>();
builder.Services.AddScoped<AppLogService>();
builder.Services.AddScoped<SeedService>();

// ------------------------------------------------------------
// Controllers + Swagger
// ------------------------------------------------------------
builder.Services.AddHttpClient();
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
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RoleEnrichmentMiddleware>();
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
