using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using POMS.Data.Models;

namespace POMS.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<AuctionPlayer> AuctionPlayers => Set<AuctionPlayer>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Player>().HasIndex(x => x.FullName);
        builder.Entity<Team>().HasIndex(x => x.ShortCode).IsUnique();
        builder.Entity<AuctionPlayer>().HasIndex(x => new { x.AuctionId, x.LotNumber }).IsUnique();
        builder.Entity<AuctionPlayer>().HasOne(x => x.Auction).WithMany(x => x.AuctionPlayers).HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AuctionPlayer>().HasOne(x => x.Player).WithMany(x => x.AuctionPlayers).HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuctionPlayer>().HasOne(x => x.Team).WithMany(x => x.AuctionPlayers).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Bid>().HasOne(x => x.AuctionPlayer).WithMany(x => x.Bids).HasForeignKey(x => x.AuctionPlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Bid>().HasOne(x => x.Team).WithMany(x => x.Bids).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Auction>().HasOne(x => x.CurrentAuctionPlayer).WithMany().HasForeignKey(x => x.CurrentAuctionPlayerId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<AuctionPlayer>().HasIndex(x => new { x.AuctionId, x.PlayerId }).IsUnique();
    }
}
