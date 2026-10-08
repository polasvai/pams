using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace POMS.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName
    {
        get; set;
    }
}

public class Player
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth
    {
        get; set;
    }
    [StringLength(60)]
    public string? Nationality
    {
        get; set;
    }
    public PlayerRole Role
    {
        get; set;
    }
    [StringLength(30)]
    public string? BattingStyle
    {
        get; set;
    }
    [StringLength(30)]
    public string? BowlingStyle
    {
        get; set;
    }
    [Range(1, 100)] public int SkillRating { get; set; } = 50;
    [Range(0, double.MaxValue)]
    public decimal BasePrice
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
    
    [StringLength(20)]
    public string? MobileNumber { get; set; }
    
    [StringLength(20)]
    public string? Category { get; set; }
    
    [Required(ErrorMessage = "Payment Method is required")]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment Number is required")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Payment Number must be exactly 11 digits")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Payment Number must contain only numbers")]
    public string PaymentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction ID is required")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Transaction ID can only contain English characters and numbers")]
    public string TxnID { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jersey Name is required")]
    [StringLength(50)]
    public string JerseyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jersey Size is required")]
    [StringLength(10)]
    public string JerseySize { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jersey Number is required")]
    [StringLength(10)]
    public string JerseyNumber { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ProfilePictureUrl { get; set; }

    public ICollection<AuctionPlayer> AuctionPlayers { get; set; } = new List<AuctionPlayer>();
}

public class Team
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(10)] public string ShortCode { get; set; } = string.Empty;
    [StringLength(300)]
    public string? LogoUrl
    {
        get; set;
    }
    public decimal TotalBudget
    {
        get; set;
    }
    public ICollection<AuctionPlayer> AuctionPlayers { get; set; } = new List<AuctionPlayer>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}

public class Auction
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    public DateTimeOffset StartsAt
    {
        get; set;
    }
    public DateTimeOffset? EndsAt
    {
        get; set;
    }
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
    [Range(0, double.MaxValue)] public decimal MinimumIncrement { get; set; } = 0;
    public int? CurrentAuctionPlayerId
    {
        get; set;
    }
    public AuctionPlayer? CurrentAuctionPlayer
    {
        get; set;
    }
    public ICollection<AuctionPlayer> AuctionPlayers { get; set; } = new List<AuctionPlayer>();
}

public class AuctionPlayer
{
    public int Id
    {
        get; set;
    }
    public int AuctionId
    {
        get; set;
    }
    public Auction Auction { get; set; } = null!;
    public int PlayerId
    {
        get; set;
    }
    public Player Player { get; set; } = null!;
    public int? TeamId
    {
        get; set;
    }
    public Team? Team
    {
        get; set;
    }
    public AuctionPlayerStatus Status { get; set; } = AuctionPlayerStatus.Pending;
    public decimal? SoldPrice
    {
        get; set;
    }
    public int LotNumber
    {
        get; set;
    }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}

public class Bid
{
    public int Id
    {
        get; set;
    }
    public int AuctionPlayerId
    {
        get; set;
    }
    public AuctionPlayer AuctionPlayer { get; set; } = null!;
    public int TeamId
    {
        get; set;
    }
    public Team Team { get; set; } = null!;
    public decimal Amount
    {
        get; set;
    }
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? PlacedByUserId
    {
        get; set;
    }
    public bool IsWinning
    {
        get; set;
    }
}

public enum MatchStatus { Scheduled, Ongoing, Completed, Abandoned }

public class Match
{
    public int Id { get; set; }
    
    public int Team1Id { get; set; }
    public Team Team1 { get; set; } = null!;

    public int Team2Id { get; set; }
    public Team Team2 { get; set; } = null!;

    [Required]
    public DateTime MatchDate { get; set; }

    [StringLength(200)]
    public string? Venue { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    [StringLength(200)]
    public string? Result { get; set; }
}
