using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;
using System.Globalization;

namespace POMS.Web.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var budgets = await db.Teams.Select(t => t.TotalBudget).ToListAsync(ct);
        var soldPrices = await db.AuctionPlayers.Where(x => x.Status == AuctionPlayerStatus.Sold).Select(x => x.SoldPrice).ToListAsync(ct);
        ViewBag.TeamCount = await db.Teams.CountAsync(ct);
        ViewBag.PlayerCount = await db.Players.CountAsync(p => p.IsActive, ct);
        ViewBag.AuctionCount = await db.Auctions.CountAsync(ct);
        ViewBag.SoldCount = await db.AuctionPlayers.CountAsync(x => x.Status == AuctionPlayerStatus.Sold, ct);
        ViewBag.TotalBudget = budgets.Sum();
        ViewBag.TotalBudgetText = budgets.Sum().ToString("N0", CultureInfo.GetCultureInfo("en-IN"));
        ViewBag.Spent = soldPrices.Sum(x => x ?? 0);
        ViewBag.SpentText = soldPrices.Sum(x => x ?? 0).ToString("N0", CultureInfo.GetCultureInfo("en-IN"));
        ViewBag.ActiveAuction = await db.Auctions.Include(a => a.CurrentAuctionPlayer).ThenInclude(x => x!.Player).FirstOrDefaultAsync(a => a.Status == AuctionStatus.Live || a.Status == AuctionStatus.Paused, ct);
        ViewBag.RecentAuctions = (await db.Auctions.ToListAsync(ct)).OrderByDescending(a => a.StartsAt).Take(5).ToList();
        return View();
    }
}
