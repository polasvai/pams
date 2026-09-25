using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/poms.db";
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<POMS.Data.Services.AuctionService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseMigrationsEndPoint();
else { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin")) await roleManager.CreateAsync(new IdentityRole("Admin"));
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByEmailAsync("admin@poms.local") is null)
    {
        var admin = new ApplicationUser { UserName = "admin@poms.local", Email = "admin@poms.local", EmailConfirmed = true, DisplayName = "System Administrator" };
        await userManager.CreateAsync(admin, "Admin@123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
    if (!await db.Teams.AnyAsync())
    {
        db.Teams.AddRange(new Team { Name = "Mumbai Mavericks", ShortCode = "MM", TotalBudget = 12000000 }, new Team { Name = "Delhi Dynamos", ShortCode = "DD", TotalBudget = 12000000 });
        db.Players.AddRange(
            new Player { FullName = "Arjun Sharma", Role = PlayerRole.Batter, Nationality = "India", BattingStyle = "Right-hand", SkillRating = 88, BasePrice = 500000 },
            new Player { FullName = "Liam Smith", Role = PlayerRole.Bowler, Nationality = "England", BowlingStyle = "Right-arm fast", SkillRating = 85, BasePrice = 400000 },
            new Player { FullName = "Kai Johnson", Role = PlayerRole.AllRounder, Nationality = "Australia", BattingStyle = "Left-hand", BowlingStyle = "Right-arm medium", SkillRating = 90, BasePrice = 750000 });
        await db.SaveChangesAsync();
    }
}
app.Run();
