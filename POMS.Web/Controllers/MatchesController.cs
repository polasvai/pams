using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;

namespace POMS.Web.Controllers;

[Authorize]
public class MatchesController : Controller
{
    private readonly ApplicationDbContext _db;

    public MatchesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var matches = await _db.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .OrderBy(m => m.MatchDate)
            .ToListAsync(ct);
            
        return View(matches);
    }

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(ct), "Id", "Name");
        return View(new Match { MatchDate = DateTime.Today.AddDays(1) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Match match, CancellationToken ct)
    {
        if (match.Team1Id == match.Team2Id)
        {
            ModelState.AddModelError("Team2Id", "A team cannot play against itself.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(ct), "Id", "Name");
            return View(match);
        }

        _db.Matches.Add(match);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Match scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var match = await _db.Matches.FindAsync(new object[] { id }, ct);
        if (match == null) return NotFound();
        
        ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(ct), "Id", "Name");
        return View(match);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Match match, CancellationToken ct)
    {
        if (id != match.Id) return NotFound();

        if (match.Team1Id == match.Team2Id)
        {
            ModelState.AddModelError("Team2Id", "A team cannot play against itself.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(ct), "Id", "Name");
            return View(match);
        }

        _db.Update(match);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Match updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var match = await _db.Matches.FindAsync(new object[] { id }, ct);
        if (match != null)
        {
            _db.Matches.Remove(match);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Match deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
