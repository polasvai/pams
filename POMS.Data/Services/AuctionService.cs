using Microsoft.EntityFrameworkCore;
using POMS.Data.Models;

namespace POMS.Data.Services;

public sealed class AuctionService(ApplicationDbContext db)
{
    public Task<Auction?> GetActiveAuctionAsync(CancellationToken ct = default) =>
        db.Auctions.Include(a => a.CurrentAuctionPlayer).ThenInclude(ap => ap!.Player)
            .AsSplitQuery().FirstOrDefaultAsync(a => a.Status == AuctionStatus.Live || a.Status == AuctionStatus.Paused, ct);

    public async Task<AuctionServiceResult> StartAsync(int id, CancellationToken ct = default)
    {
        var auction = await db.Auctions.Include(a => a.AuctionPlayers).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (auction is null) return Fail("Auction was not found.");
        if (auction.Status != AuctionStatus.Draft) return Fail("Only a draft auction can be started.");
        if (auction.AuctionPlayers.Count == 0) return Fail("Add at least one player before starting.");
        auction.Status = AuctionStatus.Live;
        auction.StartsAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok("Auction started.");
    }

    public async Task<AuctionServiceResult> SetStatusAsync(int id, AuctionStatus status, CancellationToken ct = default)
    {
        var auction = await db.Auctions.FindAsync([id], ct);
        if (auction is null) return Fail("Auction was not found.");
        if (status == AuctionStatus.Paused && auction.Status != AuctionStatus.Live) return Fail("Only a live auction can be paused.");
        if (status == AuctionStatus.Live && auction.Status != AuctionStatus.Paused) return Fail("Only a paused auction can resume.");
        auction.Status = status;
        if (status == AuctionStatus.Completed) auction.EndsAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok($"Auction is now {status}.");
    }

    public async Task<AuctionServiceResult> SelectPlayerAsync(int id, int lotId, CancellationToken ct = default)
    {
        var auction = await db.Auctions.Include(a => a.AuctionPlayers).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (auction is null) return Fail("Auction was not found.");
        if (auction.Status != AuctionStatus.Live) return Fail("The auction must be live.");
        var next = auction.AuctionPlayers.FirstOrDefault(x => x.Id == lotId);
        if (next is null || next.Status != AuctionPlayerStatus.Pending) return Fail("Select a pending player.");
        auction.CurrentAuctionPlayerId = next.Id;
        next.Status = AuctionPlayerStatus.OnAuction;
        await db.SaveChangesAsync(ct);
        return Ok("Player is now on the block.");
    }

    public async Task<AuctionServiceResult> PlaceBidAsync(int id, int lotId, int teamId, decimal amount, string? userId = null, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var auction = await db.Auctions.FindAsync([id], ct);
        if (auction is null) return Fail("Auction was not found.");
        if (auction.Status != AuctionStatus.Live || auction.CurrentAuctionPlayerId != lotId) return Fail("This player is not open for bidding.");
        var lot = await db.AuctionPlayers.Include(x => x.Player).FirstOrDefaultAsync(x => x.Id == lotId, ct);
        var team = await db.Teams.FindAsync([teamId], ct);
        if (lot is null || team is null) return Fail("The player or team was not found.");
        if (lot.Status != AuctionPlayerStatus.OnAuction) return Fail("This lot is not open for bidding.");
        var bidsForLot = await db.Bids.Where(b => b.AuctionPlayerId == lotId).ToListAsync(ct);
        var current = bidsForLot.OrderByDescending(b => b.Amount).FirstOrDefault();
        var highest = current?.Amount ?? lot.Player.BasePrice;
        if (current?.IsWinning == true && current.TeamId == teamId) return Fail("A team cannot bid against itself.");
        if (highest >= amount) return Fail($"The bid must be at least {(highest + auction.MinimumIncrement):N0}.");
        var spent = (await db.Bids.Where(b => b.TeamId == teamId && b.AuctionPlayer.AuctionId == id && b.IsWinning).ToListAsync(ct)).Sum(b => b.Amount);
        if (amount > team.TotalBudget - spent) return Fail("The team does not have enough remaining budget.");
        await db.Bids.Where(b => b.AuctionPlayerId == lotId && b.IsWinning).ExecuteUpdateAsync(s => s.SetProperty(b => b.IsWinning, false), ct);
        db.Bids.Add(new Bid { AuctionPlayerId = lotId, TeamId = teamId, Amount = amount, IsWinning = true, PlacedByUserId = userId });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok($"Bid recorded: {team.Name} at {amount:N0}.");
    }

    public async Task<AuctionServiceResult> FinishPlayerAsync(int id, bool sold, CancellationToken ct = default)
    {
        var auction = await db.Auctions.Include(a => a.CurrentAuctionPlayer).ThenInclude(x => x!.Bids).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (auction is null || auction.CurrentAuctionPlayer is null) return Fail("No player is on the block.");
        var lot = auction.CurrentAuctionPlayer;
        var winner = lot.Bids.FirstOrDefault(b => b.IsWinning);
        if (sold && winner is null) return Fail("A player cannot be sold without a winning bid.");
        lot.Status = sold ? AuctionPlayerStatus.Sold : AuctionPlayerStatus.Unsold;
        if (sold) { lot.SoldPrice = winner!.Amount; lot.TeamId = winner.TeamId; }
        auction.CurrentAuctionPlayerId = null;
        await db.SaveChangesAsync(ct);
        return Ok(sold ? "Player sold." : "Player marked unsold.");
    }

    private static AuctionServiceResult Ok(string message) => new(true, message);
    private static AuctionServiceResult Fail(string message) => new(false, message);
}

public sealed record AuctionServiceResult(bool Succeeded, string Message);
