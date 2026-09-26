using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;
using POMS.Data.Services;

namespace POMS.Tests;

public sealed class AuctionServiceTests
{
    [Fact]
    public async Task First_bid_must_meet_increment_over_base_price()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.PlaceBidAsync(fixture.Auction.Id, fixture.Lot.Id, fixture.TeamA.Id, 1000);

        Assert.False(result.Succeeded);
        Assert.Contains("100,500", result.Message);
        Assert.Empty(await fixture.Db.Bids.ToListAsync());
    }

    [Fact]
    public async Task Team_cannot_bid_against_itself()
    {
        await using var fixture = await Fixture.CreateAsync();
        var first = await fixture.Service.PlaceBidAsync(fixture.Auction.Id, fixture.Lot.Id, fixture.TeamA.Id, 100500);

        Assert.True(first.Succeeded);
        var second = await fixture.Service.PlaceBidAsync(fixture.Auction.Id, fixture.Lot.Id, fixture.TeamA.Id, 101000);

        Assert.False(second.Succeeded);
        Assert.Contains("against itself", second.Message);
    }

    [Fact]
    public async Task Team_bid_cannot_exceed_remaining_budget()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.PlaceBidAsync(fixture.Auction.Id, fixture.Lot.Id, fixture.TeamA.Id, 2_000_000);

        Assert.False(result.Succeeded);
        Assert.Contains("remaining budget", result.Message);
    }

    [Fact]
    public async Task Sold_player_requires_winning_bid_and_records_team()
    {
        await using var fixture = await Fixture.CreateAsync();
        var rejected = await fixture.Service.FinishPlayerAsync(fixture.Auction.Id, true);
        Assert.False(rejected.Succeeded);

        await fixture.Service.PlaceBidAsync(fixture.Auction.Id, fixture.Lot.Id, fixture.TeamA.Id, 100500);
        var sold = await fixture.Service.FinishPlayerAsync(fixture.Auction.Id, true);
        var lot = await fixture.Db.AuctionPlayers.SingleAsync();

        Assert.True(sold.Succeeded);
        Assert.Equal(AuctionPlayerStatus.Sold, lot.Status);
        Assert.Equal(100500, lot.SoldPrice);
        Assert.Equal(fixture.TeamA.Id, lot.TeamId);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db
        {
            get;
        }
        public AuctionService Service
        {
            get;
        }
        public Auction Auction { get; } = new() { Name = "Test Auction", Status = AuctionStatus.Live, MinimumIncrement = 500 };
        public Player Player { get; } = new() { FullName = "Test Player", BasePrice = 100000, SkillRating = 80 };
        public Team TeamA { get; } = new() { Name = "Team A", ShortCode = "TA", TotalBudget = 1_000_000 };
        public AuctionPlayer Lot { get; } = new() { Status = AuctionPlayerStatus.OnAuction, LotNumber = 1 };

        private Fixture(SqliteConnection connection)
        {
            this.connection = connection;
            Db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            Service = new AuctionService(Db);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture(new SqliteConnection("DataSource=:memory:"));
            await fixture.connection.OpenAsync();
            await fixture.Db.Database.EnsureCreatedAsync();
            fixture.Db.AddRange(fixture.TeamA, new Team { Name = "Team B", ShortCode = "TB", TotalBudget = 1_000_000 }, fixture.Player, fixture.Auction);
            await fixture.Db.SaveChangesAsync();
            fixture.Lot.AuctionId = fixture.Auction.Id;
            fixture.Lot.PlayerId = fixture.Player.Id;
            fixture.Db.AuctionPlayers.Add(fixture.Lot);
            await fixture.Db.SaveChangesAsync();
            fixture.Auction.CurrentAuctionPlayerId = fixture.Lot.Id;
            await fixture.Db.SaveChangesAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
