using System.Text;
using Azure.Identity;
using GameStore.Data;
using GameStore.Models.Entities;
using GameStore.Models.Enums;
using GameStore.Repositories;
using GameStore.Repositories.Impl;
using GameStore.Services;
using GameStore.Services.Impl;
using GameStore.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration provider
var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];

builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUrl),
    new DefaultAzureCredential()
);

// Add PostgreSQL database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Add BlobService
builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
    var containerName = builder.Configuration["AzureBlobContainerName"];
    return new BlobService(connectionString, containerName);
});

// Register services
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<JwtUtil>();
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()    // Allow requests from any origin
               .AllowAnyMethod()    // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
               .AllowAnyHeader();   // Allow all headers
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:5173") // Specify allowed origin
               .WithMethods("GET", "POST", "PUT", "DELETE") // Specify allowed methods
               .AllowAnyHeader() // Allow all headers
               .AllowCredentials(); // Allow credentials
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Enum.GetNames(typeof(RoleName)))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await SeedAdminAccount(userManager, roleManager, logger, configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseCors("LocalPolicy");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();

// Run the application and navigate to the /swagger/index.html URL to see the Swagger UI.
// You can use the Swagger UI to test the API endpoints.
// You can also use Postman or another API testing tool to test the API endpoints.

// Create default admin account
static async Task SeedAdminAccount(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger logger, IConfiguration configuration)
{
    const string adminUserName = "hristo";
    const string adminEmail = "hsivov@gmail.com";
    var adminPassword = configuration["AdminPassword"];

    if (await userManager.FindByNameAsync(adminUserName) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            Role = RoleName.Admin,
            Email = adminEmail,
            FirstName = "Hristo",
            LastName = "Sivov",
            Age = 47,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, RoleName.Admin.ToString());
            logger.LogInformation("Default admin account created.");
        }
        else
        {
            logger.LogError("An error occurred while creating the default admin account.");
            foreach (var error in result.Errors)
            {
                logger.LogError(error.Description);
            }
        }
    }
    else
    {
        logger.LogInformation("Default admin account already exists.");
    }
}