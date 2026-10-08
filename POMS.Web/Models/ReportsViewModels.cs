using POMS.Data.Models;

namespace POMS.Web.Models;

public class PlayerCategoryListViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public List<CategoryGroup> CategoryGroups { get; set; } = new();
    public int TotalPlayers => CategoryGroups.Sum(g => g.Players.Count);
}

public class CategoryGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public string DisplayTitle => $"{CategoryName} Category Player List";
    public List<Player> Players { get; set; } = new();
}

public class TeamDetailsReportViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public List<Team> AllTeams { get; set; } = new();
    public int? SelectedTeamId { get; set; }
    public bool IsAllTeams { get; set; }
    public Team? SelectedTeam { get; set; }
    public List<Player> TeamPlayers { get; set; } = new();
    public List<TeamGroupReport> AllTeamGroups { get; set; } = new();
}

public class TeamGroupReport
{
    public Team Team { get; set; } = null!;
    public List<Player> Players { get; set; } = new();
}

