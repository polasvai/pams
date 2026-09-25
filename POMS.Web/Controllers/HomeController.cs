using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;
using POMS.Web.Models;
using System.Globalization;

namespace POMS.Web.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.TeamCount = await db.Teams.CountAsync(ct);
        ViewBag.PlayerCount = await db.Players.CountAsync(p => p.IsActive, ct);
        ViewBag.AuctionCount = await db.Auctions.CountAsync(ct);
        var totalBudget = (await db.Teams.Select(t => t.TotalBudget).ToListAsync(ct)).Sum();
        ViewBag.TotalBudget = totalBudget;
        ViewBag.TotalBudgetText = totalBudget.ToString("N0", CultureInfo.GetCultureInfo("en-IN"));
        ViewBag.Spent = (await db.AuctionPlayers.Where(x => x.Status == AuctionPlayerStatus.Sold).Select(x => x.SoldPrice).ToListAsync(ct)).Sum(x => x ?? 0);
        ViewBag.ActiveAuction = await db.Auctions.Include(a => a.CurrentAuctionPlayer).ThenInclude(x => x!.Player).FirstOrDefaultAsync(a => a.Status == AuctionStatus.Live || a.Status == AuctionStatus.Paused, ct);
        ViewBag.RecentAuctions = (await db.Auctions.ToListAsync(ct)).OrderByDescending(a => a.StartsAt).Take(5).ToList();
        return View();
    }
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}

