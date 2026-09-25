using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// Ensure App_Data path is resolved reliably relative to ContentRootPath
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);
var dbPath = Path.Combine(appDataPath, "poms.db");
var connectionString = $"Data Source={dbPath};Cache=Shared";

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "POMS.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<POMS.Data.Services.AuctionService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirect legacy /Identity/Account paths to /Account
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/Login" + context.Request.QueryString);
        return;
    }
    if (path.StartsWith("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/Register" + context.Request.QueryString);
        return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Initialize and Seed Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "SuperAdmin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    async Task SeedOrUpdateUserAsync(string username, string email, string displayName, string password)
    {
        var user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }
        }
        else
        {
            // Ensure password is set & verified
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);
            if (!await userManager.IsInRoleAsync(user, "Admin")) await userManager.AddToRoleAsync(user, "Admin");
            if (!await userManager.IsInRoleAsync(user, "SuperAdmin")) await userManager.AddToRoleAsync(user, "SuperAdmin");
        }
    }

    await SeedOrUpdateUserAsync("superadmin", "superadmin@poms.local", "Super Administrator", "Admin@123!");
    await SeedOrUpdateUserAsync("admin", "admin@poms.local", "System Administrator", "Admin@123!");

    if (!await db.Teams.AnyAsync())
    {
        db.Teams.AddRange(
            new Team { Name = "Mumbai Mavericks", ShortCode = "MM", TotalBudget = 150000000 },
            new Team { Name = "Delhi Dynamos", ShortCode = "DD", TotalBudget = 140000000 },
            new Team { Name = "Chennai Champions", ShortCode = "CC", TotalBudget = 160000000 },
            new Team { Name = "Bangalore Blasters", ShortCode = "BB", TotalBudget = 135000000 }
        );

        db.Players.AddRange(
            new Player { FullName = "Virat Kohli", Role = PlayerRole.Batter, Nationality = "India", BattingStyle = "Right-hand", SkillRating = 96, BasePrice = 20000000 },
            new Player { FullName = "Jasprit Bumrah", Role = PlayerRole.Bowler, Nationality = "India", BowlingStyle = "Right-arm fast", SkillRating = 97, BasePrice = 20000000 },
            new Player { FullName = "Hardik Pandya", Role = PlayerRole.AllRounder, Nationality = "India", BattingStyle = "Right-hand", BowlingStyle = "Right-arm medium-fast", SkillRating = 91, BasePrice = 15000000 },
            new Player { FullName = "Heinrich Klaasen", Role = PlayerRole.Wicketkeeper, Nationality = "South Africa", BattingStyle = "Right-hand", SkillRating = 92, BasePrice = 12000000 },
            new Player { FullName = "Pat Cummins", Role = PlayerRole.AllRounder, Nationality = "Australia", BattingStyle = "Right-hand", BowlingStyle = "Right-arm fast", SkillRating = 94, BasePrice = 18000000 },
            new Player { FullName = "Rashid Khan", Role = PlayerRole.Bowler, Nationality = "Afghanistan", BowlingStyle = "Right-arm legbreak", SkillRating = 95, BasePrice = 15000000 }
        );

        await db.SaveChangesAsync();
    }
}

app.Run();
