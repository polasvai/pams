namespace POMS.Data.Models;

public enum PlayerRole
{
    Batter,
    Bowler,
    AllRounder,
    Wicketkeeper
}

public enum AuctionStatus
{
    Draft,
    Live,
    Paused,
    Completed,
    Cancelled
}

public enum AuctionPlayerStatus
{
    Pending,
    OnAuction,
    Sold,
    Unsold
}
