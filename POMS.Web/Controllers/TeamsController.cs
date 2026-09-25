using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

namespace POMS.Web.Controllers;

[Authorize]
public class TeamsController(ApplicationDbContext db) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var teams = await db.Teams
            .Include(t => t.AuctionPlayers)
            .OrderBy(t => t.Name)
            .ToListAsync();
        return View(teams);
    }
    public IActionResult Create() => View(new Team());
    [HttpPost] public async Task<IActionResult> Create(Team team, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(team);
        db.Teams.Add(team); await db.SaveChangesAsync(ct); TempData["Success"] = "Team created."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) => View(await db.Teams.FindAsync(id));
    [HttpPost] public async Task<IActionResult> Edit(int id, Team team, CancellationToken ct)
    {
        if (id != team.Id) return NotFound();
        if (!ModelState.IsValid) return View(team);
        db.Update(team); await db.SaveChangesAsync(ct); TempData["Success"] = "Team updated."; return RedirectToAction(nameof(Index));
    }
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var team = await db.Teams
            .Include(t => t.AuctionPlayers)
            .ThenInclude(a => a.Player)
            .Include(t => t.AuctionPlayers)
            .ThenInclude(a => a.Auction)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        return team is null ? NotFound() : View(team);
    }
}
