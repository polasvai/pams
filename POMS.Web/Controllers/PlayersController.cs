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
        return View(await db.Players.Where(p => string.IsNullOrWhiteSpace(search) || p.FullName.Contains(search) || (p.MobileNumber != null && p.MobileNumber.Contains(search))).OrderBy(p => p.FullName).ToListAsync());
    }
    public IActionResult Create() => View(new Player());
    [HttpPost]
    public async Task<IActionResult> Create(Player player, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(player);
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
        return RedirectToAction("Index", "Home");
    }
    public async Task<IActionResult> Edit(int id) => View(await db.Players.FindAsync(id));
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Player player, CancellationToken ct)
    {
        if (id != player.Id)
            return NotFound();
        if (!ModelState.IsValid)
            return View(player);
        db.Update(player);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Player updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var player = await db.Players.FindAsync(id);
        if (player is not null)
        {
            player.IsActive = false;
            await db.SaveChangesAsync(ct);
        }
        TempData["Success"] = "Player archived.";
        return RedirectToAction(nameof(Index));
    }
}
