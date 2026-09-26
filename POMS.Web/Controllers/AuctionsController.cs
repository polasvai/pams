using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;
using POMS.Data.Services;

namespace POMS.Web.Controllers;

[Authorize]
public class AuctionsController(ApplicationDbContext db, AuctionService auctionService) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index() => View((await db.Auctions.Include(a => a.AuctionPlayers).ToListAsync()).OrderByDescending(a => a.StartsAt).ToList());
    public IActionResult Create() => View(new Auction());
    [HttpPost]
    public async Task<IActionResult> Create(Auction auction, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(auction);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Auction created.";
        return RedirectToAction(nameof(Index));
    }
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var auction = await db.Auctions.Include(a => a.AuctionPlayers).ThenInclude(x => x.Player).FirstOrDefaultAsync(a => a.Id == id);
        return auction is null ? NotFound() : View(auction);
    }
    [HttpPost]
    public async Task<IActionResult> AddPlayers(int id, int[] playerIds, CancellationToken ct)
    {
        var auction = await db.Auctions.Include(a => a.AuctionPlayers).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (auction is null || auction.Status != AuctionStatus.Draft)
        {
            TempData["Error"] = "Players can only be added to draft auctions.";
            return RedirectToAction(nameof(Details), new
            {
                id
            });
        }
        var nextLot = auction.AuctionPlayers.Count + 1;
        foreach (var playerId in (playerIds ?? Array.Empty<int>()).Distinct())
            if (!await db.AuctionPlayers.AnyAsync(x => x.AuctionId == id && x.PlayerId == playerId, ct))
                db.AuctionPlayers.Add(new AuctionPlayer { AuctionId = id, PlayerId = playerId, LotNumber = nextLot++ });
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Players added to auction.";
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
    [HttpPost]
    public async Task<IActionResult> Start(int id, CancellationToken ct)
    {
        var result = await auctionService.StartAsync(id, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
    [HttpPost]
    public async Task<IActionResult> Status(int id, AuctionStatus status, CancellationToken ct)
    {
        var result = await auctionService.SetStatusAsync(id, status, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
    [AllowAnonymous]
    public async Task<IActionResult> Live(int? id)
    {
        var auction = id is null
            ? await auctionService.GetActiveAuctionAsync()
            : await db.Auctions
                .Include(a => a.AuctionPlayers).ThenInclude(x => x.Player)
                .Include(a => a.CurrentAuctionPlayer).ThenInclude(x => x!.Player)
                .FirstOrDefaultAsync(a => a.Id == id);

        if (id is not null && auction is null)
            return NotFound();

        ViewBag.Teams = await db.Teams.OrderBy(t => t.Name).ToListAsync();
        ViewBag.PendingLots = auction is null
            ? new List<AuctionPlayer>()
            : await db.AuctionPlayers.Where(x => x.AuctionId == auction.Id && x.Status == AuctionPlayerStatus.Pending).Include(x => x.Player).OrderBy(x => x.LotNumber).ToListAsync();
        ViewBag.Bids = auction is null
            ? new List<Bid>()
            : (await db.Bids.Where(b => b.AuctionPlayer.AuctionId == auction.Id && b.AuctionPlayerId == auction.CurrentAuctionPlayerId).Include(b => b.Team).ToListAsync()).OrderByDescending(b => b.Amount).ToList();
        return View(auction);
    }
    [HttpPost]
    public async Task<IActionResult> SelectPlayer(int id, int lotId, CancellationToken ct)
    {
        var result = await auctionService.SelectPlayerAsync(id, lotId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Live), new
        {
            id
        });
    }
    [HttpPost]
    public async Task<IActionResult> PlaceBid(int id, int lotId, int teamId, decimal amount, CancellationToken ct)
    {
        var result = await auctionService.PlaceBidAsync(id, lotId, teamId, amount, User.Identity?.Name, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Live), new
        {
            id
        });
    }
    [HttpPost]
    public async Task<IActionResult> FinishPlayer(int id, bool sold, CancellationToken ct)
    {
        var result = await auctionService.FinishPlayerAsync(id, sold, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Live), new
        {
            id
        });
    }
}
