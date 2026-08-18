using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskHub.Data;
using TaskHub.Models;
using TaskHub.Options;
using TaskHub.Services;

var builder = WebApplication.CreateBuilder(args);

// OPTIONAL AZURE KEY VAULT STEP
// Uncomment this block when you are ready to load secrets
// from Azure Key Vault instead of storing them locally.
// ============================================================

/*
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = builder.Configuration["ManagedIdentityClientId"]
    }));
*/

// ============================================================
// CONNECTION STRING
// Try Key Vault first (later assignment step), then fall back
// to appsettings.json (early assignment step).
// ============================================================

var connectionStringSecretName = builder.Configuration["ConnectionStringSecretName"];

string? connectionString = null;

if (!string.IsNullOrWhiteSpace(connectionStringSecretName))
{
    connectionString = builder.Configuration[connectionStringSecretName];
}

connectionString ??= builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string was not found in Azure Key Vault or appsettings.json.");
}

// ============================================================
// JWT OPTIONS
// Load normal JWT settings from appsettings.json first.
// Then override only the SigningKey from Azure Key Vault
// when that later assignment step is enabled.
// ============================================================

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

var jwtSigningKeySecretName = builder.Configuration["JwtSigningKeySecretName"];

if (!string.IsNullOrWhiteSpace(jwtSigningKeySecretName))
{
    var keyVaultSigningKey = builder.Configuration[jwtSigningKeySecretName];

    if (!string.IsNullOrWhiteSpace(keyVaultSigningKey))
    {
        jwtOptions.SigningKey = keyVaultSigningKey;
    }
}

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("JWT signing key is required.");
}

builder.Services.AddSingleton(Options.Create(jwtOptions));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred."
        };

        return new BadRequestObjectResult(problemDetails);
    };
});

// ============================================================
// Uncomment this block when you add auth cookie persistence to file system 
// ============================================================
// builder.Services
//     .AddDataProtection()
//     .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
//     .SetApplicationName("MyApp");



builder.Services.AddHealthChecks();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// ============================================================
// Uncomment this block when you are ready to perform the initial database migration step in K8S 
// ============================================================
// if (args.Contains("--migrate-only"))
// {
//     using var scope = app.Services.CreateScope();
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     db.Database.Migrate();
//     return;
// }

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
