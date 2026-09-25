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
        return View(await db.Players.Where(p => string.IsNullOrWhiteSpace(search) || p.FullName.Contains(search)).OrderBy(p => p.FullName).ToListAsync());
    }
    public IActionResult Create() => View(new Player());
    [HttpPost] public async Task<IActionResult> Create(Player player, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(player);
        db.Players.Add(player); await db.SaveChangesAsync(ct); TempData["Success"] = "Player created."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) => View(await db.Players.FindAsync(id));
    [HttpPost] public async Task<IActionResult> Edit(int id, Player player, CancellationToken ct)
    {
        if (id != player.Id) return NotFound();
        if (!ModelState.IsValid) return View(player);
        db.Update(player); await db.SaveChangesAsync(ct); TempData["Success"] = "Player updated."; return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var player = await db.Players.FindAsync(id);
        if (player is not null) { player.IsActive = false; await db.SaveChangesAsync(ct); }
        TempData["Success"] = "Player archived."; return RedirectToAction(nameof(Index));
    }
}
