using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

using POMS.Data.Services;

namespace POMS.Web.Controllers;

[Authorize]
public class TeamsController(ApplicationDbContext db, AuctionService auctionService) : Controller
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
    [HttpPost]
    public async Task<IActionResult> Create(Team team, IFormFile? logoFile, [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(team);
            
        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "teams");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "team_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(logoFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }
            team.LogoUrl = "/uploads/teams/" + fileName;
        }
            
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Team created.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) => View(await db.Teams.FindAsync(id));
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Team team, IFormFile? logoFile, [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (id != team.Id)
            return NotFound();
        if (!ModelState.IsValid)
            return View(team);
            
        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "teams");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "team_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(logoFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }
            team.LogoUrl = "/uploads/teams/" + fileName;
        }
        else
        {
            // Preserve existing logo if not updated
            var existingTeam = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (existingTeam != null && string.IsNullOrEmpty(team.LogoUrl))
            {
                team.LogoUrl = existingTeam.LogoUrl;
            }
        }
            
        db.Update(team);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Team updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var team = await db.Teams.Include(t => t.AuctionPlayers).Include(t => t.Bids).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (team is not null)
        {
            if (team.AuctionPlayers.Any() || team.Bids.Any())
            {
                TempData["Error"] = "Cannot delete this team because it has associated auction players or bids.";
            }
            else
            {
                db.Teams.Remove(team);
                await db.SaveChangesAsync(ct);
                TempData["Success"] = "Team deleted successfully.";
            }
        }
        return RedirectToAction(nameof(Index));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePlayer(int teamId, int auctionPlayerId, CancellationToken ct)
    {
        var result = await auctionService.RemovePlayerFromTeamAsync(teamId, auctionPlayerId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = teamId });
    }
}
