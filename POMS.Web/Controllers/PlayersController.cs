using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

namespace POMS.Web.Controllers;

[Authorize]
public class PlayersController(ApplicationDbContext db) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        return View(await db.Players.Where(p => string.IsNullOrWhiteSpace(search) || p.FullName.Contains(search) || (p.MobileNumber != null && p.MobileNumber.Contains(search))).OrderBy(p => p.Id).ToListAsync());
    }
    public IActionResult Create() => View(new Player());
    [HttpPost]
    public async Task<IActionResult> Create(Player player, IFormFile? profilePic, [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(player.MobileNumber))
        {
            if (await db.Players.AnyAsync(p => p.MobileNumber == player.MobileNumber, ct))
            {
                ModelState.AddModelError(nameof(Player.MobileNumber), "This mobile number is already registered.");
            }
        }

        if (!ModelState.IsValid)
            return View(player);
            
        if (profilePic != null && profilePic.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "players");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "player_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(profilePic.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePic.CopyToAsync(stream);
            }
            player.ProfilePictureUrl = "/uploads/players/" + fileName;
        }
            
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Player created.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new Player());

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(Player player, IFormFile? profilePic, [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        ModelState.Remove(nameof(Player.SkillRating));
        ModelState.Remove(nameof(Player.BasePrice));

        if (!string.IsNullOrEmpty(player.MobileNumber))
        {
            if (await db.Players.AnyAsync(p => p.MobileNumber == player.MobileNumber, ct))
            {
                ModelState.AddModelError(nameof(Player.MobileNumber), "This mobile number is already registered.");
            }
        }

        if (!ModelState.IsValid)
            return View(player);

        if (profilePic != null && profilePic.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "players");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "player_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(profilePic.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePic.CopyToAsync(stream);
            }
            player.ProfilePictureUrl = "/uploads/players/" + fileName;
        }

        player.IsActive = false; // Admin will activate later
        player.SkillRating = 50; // Default
        player.BasePrice = 0;    // Default

        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Your registration was successful! Your profile is pending activation by the admin.";
        return RedirectToAction(nameof(RegistrationSuccess), new { id = player.Id });
    }

    [AllowAnonymous]
    public async Task<IActionResult> RegistrationSuccess(int id)
    {
        var player = await db.Players.FindAsync(id);
        if (player == null) return NotFound();
        return View(player);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Print(int id)
    {
        var player = await db.Players.FindAsync(id);
        if (player == null) return NotFound();
        return View(player);
    }

    [AllowAnonymous]
    public IActionResult DownloadCopy() => View();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadCopy(int id, string mobileNumber)
    {
        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == id && p.MobileNumber == mobileNumber);
        if (player == null)
        {
            ModelState.AddModelError("", "No registration found with this ID and Mobile Number combination.");
            return View();
        }
        return RedirectToAction(nameof(Print), new { id = player.Id });
    }

    public async Task<IActionResult> Edit(int id) => View(await db.Players.FindAsync(id));
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Player player, IFormFile? profilePic, [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (id != player.Id)
            return NotFound();
            
        if (!string.IsNullOrEmpty(player.MobileNumber))
        {
            if (await db.Players.AnyAsync(p => p.MobileNumber == player.MobileNumber && p.Id != player.Id, ct))
            {
                ModelState.AddModelError(nameof(Player.MobileNumber), "This mobile number is already registered to another player.");
            }
        }

        if (!ModelState.IsValid)
            return View(player);
            
        if (profilePic != null && profilePic.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "players");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "player_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(profilePic.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePic.CopyToAsync(stream);
            }
            player.ProfilePictureUrl = "/uploads/players/" + fileName;
        }
        else
        {
            var existingPlayer = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (existingPlayer != null && string.IsNullOrEmpty(player.ProfilePictureUrl))
            {
                player.ProfilePictureUrl = existingPlayer.ProfilePictureUrl;
            }
        }
            
        db.Update(player);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Player updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var player = await db.Players.FindAsync(id);
        if (player != null)
        {
            player.IsActive = !player.IsActive;
            await db.SaveChangesAsync(ct);
            TempData["Success"] = $"Player {(player.IsActive ? "activated" : "deactivated")} successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var player = await db.Players.Include(p => p.AuctionPlayers).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (player is not null)
        {
            if (player.AuctionPlayers.Any())
            {
                TempData["Error"] = "Cannot delete player because they are already part of an auction. Deactivate them instead.";
            }
            else
            {
                db.Players.Remove(player);
                await db.SaveChangesAsync(ct);
                TempData["Success"] = "Player deleted permanently.";
            }
        }
        return RedirectToAction(nameof(Index));
    }
}
