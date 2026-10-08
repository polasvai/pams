using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POMS.Data;
using POMS.Data.Models;
using POMS.Web.Models;
using POMS.Web.Services;

namespace POMS.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly SettingsService _settingsService;

    public ReportsController(ApplicationDbContext db, SettingsService settingsService)
    {
        _db = db;
        _settingsService = settingsService;
    }

    // 1. Player Category List Report
    public async Task<IActionResult> PlayerCategoryList()
    {
        var settings = _settingsService.GetSettings();
        var allPlayers = await _db.Players.OrderBy(p => p.FullName).ToListAsync();

        var soldLots = await _db.AuctionPlayers
            .Where(ap => ap.Status == AuctionPlayerStatus.Sold && ap.TeamId.HasValue)
            .Include(ap => ap.Team)
            .ToListAsync();
        var playerTeamMap = soldLots
            .GroupBy(ap => ap.PlayerId)
            .ToDictionary(g => g.Key, g => g.First().Team);

        var preferredOrder = new List<string> { "Icon", "A", "B", "C", "D", "Local", "Foreign" };

        var categoryGroups = allPlayers
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Unassigned" : p.Category.Trim())
            .OrderBy(g =>
            {
                var idx = preferredOrder.FindIndex(o => o.Equals(g.Key, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : 999;
            })
            .ThenBy(g => g.Key)
            .Select(g => new CategoryGroup
            {
                CategoryName = g.Key,
                Players = g.OrderBy(p => p.Id).Select(p => new CategoryPlayerItem
                {
                    Player = p,
                    CurrentTeam = playerTeamMap.TryGetValue(p.Id, out var team) ? team : null
                }).ToList()
            })
            .ToList();

        // If categories like Icon, A, B, C are completely empty, still ensure standard categories appear
        foreach (var standardCat in new[] { "Icon", "A", "B", "C" })
        {
            if (!categoryGroups.Any(g => g.CategoryName.Equals(standardCat, StringComparison.OrdinalIgnoreCase)))
            {
                var insertIndex = preferredOrder.FindIndex(o => o.Equals(standardCat, StringComparison.OrdinalIgnoreCase));
                categoryGroups.Add(new CategoryGroup
                {
                    CategoryName = standardCat,
                    Players = new List<CategoryPlayerItem>()
                });
            }
        }

        categoryGroups = categoryGroups
            .OrderBy(g =>
            {
                var idx = preferredOrder.FindIndex(o => o.Equals(g.CategoryName, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : 999;
            })
            .ThenBy(g => g.CategoryName)
            .ToList();

        var viewModel = new PlayerCategoryListViewModel
        {
            Settings = settings,
            CategoryGroups = categoryGroups
        };

        return View(viewModel);
    }

    // 2. Team Details Report (Select Team first or ALL Teams)
    public async Task<IActionResult> TeamDetails(int? teamId)
    {
        var settings = _settingsService.GetSettings();
        var allTeams = await _db.Teams.OrderBy(t => t.Name).ToListAsync();

        Team? selectedTeam = null;
        List<TeamPlayerItem> teamPlayers = new();
        List<TeamGroupReport> allTeamGroups = new();
        bool isAllTeams = teamId == -1;

        if (isAllTeams)
        {
            // Load all teams and their acquired auction players including auction details
            var allAuctionPlayers = await _db.AuctionPlayers
                .Where(ap => ap.TeamId.HasValue)
                .Include(ap => ap.Player)
                .Include(ap => ap.Auction)
                .OrderBy(ap => ap.LotNumber)
                .ThenBy(ap => ap.Player.FullName)
                .ToListAsync();

            foreach (var team in allTeams)
            {
                var players = allAuctionPlayers
                    .Where(ap => ap.TeamId == team.Id)
                    .GroupBy(ap => ap.PlayerId)
                    .Select(g => g.First())
                    .Select(ap => new TeamPlayerItem
                    {
                        Player = ap.Player,
                        Auction = ap.Auction,
                        SoldPrice = ap.SoldPrice,
                        LotNumber = ap.LotNumber
                    })
                    .ToList();

                allTeamGroups.Add(new TeamGroupReport
                {
                    Team = team,
                    Players = players
                });
            }
        }
        else if (teamId.HasValue && teamId.Value > 0)
        {
            selectedTeam = allTeams.FirstOrDefault(t => t.Id == teamId.Value) 
                ?? await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId.Value);

            if (selectedTeam != null)
            {
                var auctionPlayers = await _db.AuctionPlayers
                    .Where(ap => ap.TeamId == selectedTeam.Id)
                    .Include(ap => ap.Player)
                    .Include(ap => ap.Auction)
                    .OrderBy(ap => ap.LotNumber)
                    .ThenBy(ap => ap.Player.FullName)
                    .ToListAsync();

                teamPlayers = auctionPlayers
                    .GroupBy(ap => ap.PlayerId)
                    .Select(g => g.First())
                    .Select(ap => new TeamPlayerItem
                    {
                        Player = ap.Player,
                        Auction = ap.Auction,
                        SoldPrice = ap.SoldPrice,
                        LotNumber = ap.LotNumber
                    })
                    .ToList();
            }
        }

        var viewModel = new TeamDetailsReportViewModel
        {
            Settings = settings,
            AllTeams = allTeams,
            SelectedTeamId = teamId,
            IsAllTeams = isAllTeams,
            SelectedTeam = selectedTeam,
            TeamPlayers = teamPlayers,
            AllTeamGroups = allTeamGroups
        };

        return View(viewModel);
    }

    // Export Player Category List to Excel
    public async Task<IActionResult> ExportPlayerCategoryExcel()
    {
        var settings = _settingsService.GetSettings();
        var allPlayers = await _db.Players.OrderBy(p => p.FullName).ToListAsync();
        var soldLots = await _db.AuctionPlayers
            .Where(ap => ap.Status == AuctionPlayerStatus.Sold && ap.TeamId.HasValue)
            .Include(ap => ap.Team)
            .ToListAsync();
        var playerTeamMap = soldLots
            .GroupBy(ap => ap.PlayerId)
            .ToDictionary(g => g.Key, g => g.First().Team);

        var preferredOrder = new List<string> { "Icon", "A", "B", "C", "D", "Local", "Foreign" };

        var categoryGroups = allPlayers
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Unassigned" : p.Category.Trim())
            .OrderBy(g =>
            {
                var idx = preferredOrder.FindIndex(o => o.Equals(g.Key, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : 999;
            })
            .ThenBy(g => g.Key)
            .Select(g => new CategoryGroup
            {
                CategoryName = g.Key,
                Players = g.OrderBy(p => p.Id).Select(p => new CategoryPlayerItem
                {
                    Player = p,
                    CurrentTeam = playerTeamMap.TryGetValue(p.Id, out var team) ? team : null
                }).ToList()
            })
            .ToList();

        foreach (var standardCat in new[] { "Icon", "A", "B", "C" })
        {
            if (!categoryGroups.Any(g => g.CategoryName.Equals(standardCat, StringComparison.OrdinalIgnoreCase)))
            {
                categoryGroups.Add(new CategoryGroup
                {
                    CategoryName = standardCat,
                    Players = new List<CategoryPlayerItem>()
                });
            }
        }

        categoryGroups = categoryGroups
            .OrderBy(g =>
            {
                var idx = preferredOrder.FindIndex(o => o.Equals(g.CategoryName, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : 999;
            })
            .ThenBy(g => g.CategoryName)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Player Category List");
        ws.ShowGridLines = true;

        int currentRow = 2;

        // Top Main Header Box: BCL Player List 2026, Session 14
        var titleRange = ws.Range(currentRow, 1, currentRow, 9);
        titleRange.Merge().Value = "BCL Player List 2026, Session 14";
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(currentRow).Height = 30;
        currentRow += 2;

        foreach (var group in categoryGroups)
        {
            // Category Section Title
            var sectionRange = ws.Range(currentRow, 1, currentRow, 9);
            sectionRange.Merge().Value = group.DisplayTitle;
            sectionRange.Style.Font.Bold = true;
            sectionRange.Style.Font.FontSize = 12;
            sectionRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sectionRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sectionRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F7");
            sectionRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(currentRow).Height = 24;
            currentRow++;

            // Table Header Row
            string[] headers = ["SL", "Player Regi. No.", "Player Image", "Player Name", "Role", "Batting Style", "Bowling Style", "Mobile", "Current Team"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Fill.BackgroundColor = XLColor.White;
            }
            ws.Row(currentRow).Height = 22;
            currentRow++;

            // Rows
            if (group.Players.Count > 0)
            {
                int sl = 1;
                foreach (var item in group.Players)
                {
                    var p = item.Player;
                    var teamName = item.CurrentTeam != null 
                        ? (!string.IsNullOrEmpty(item.CurrentTeam.ShortCode) ? $"{item.CurrentTeam.Name} ({item.CurrentTeam.ShortCode})" : item.CurrentTeam.Name) 
                        : "Unsold";

                    ws.Cell(currentRow, 1).Value = sl++;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 2).Value = p.Id.ToString();
                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 3).Value = !string.IsNullOrEmpty(p.ProfilePictureUrl) ? "[Photo]" : "-";
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 4).Value = p.FullName;
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Cell(currentRow, 5).Value = p.Role.ToString();
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 6).Value = p.BattingStyle ?? "-";
                    ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 7).Value = p.BowlingStyle ?? "-";
                    ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 8).Value = p.MobileNumber ?? "-";
                    ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 9).Value = teamName;
                    ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    for (int c = 1; c <= 9; c++)
                    {
                        ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 20;
                    currentRow++;
                }
            }
            else
            {
                // Blank rows if empty
                for (int emptyRow = 1; emptyRow <= 3; emptyRow++)
                {
                    ws.Cell(currentRow, 1).Value = emptyRow;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    for (int c = 1; c <= 9; c++)
                    {
                        ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 20;
                    currentRow++;
                }
            }

            // Gap between category tables
            currentRow++;
        }

        ws.Columns(1, 9).AdjustToContents(8.0, 40.0);
        ws.Column(1).Width = 6;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 13;
        ws.Column(4).Width = 32;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 15;
        ws.Column(7).Width = 15;
        ws.Column(8).Width = 18;
        ws.Column(9).Width = 18;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BCL_Player_Category_List_2026.xlsx");
    }

    // Export Team Details to Excel (Single Team or ALL Teams)
    public async Task<IActionResult> ExportTeamDetailsExcel(int? teamId)
    {
        if (!teamId.HasValue || (teamId.Value <= 0 && teamId.Value != -1))
        {
            return RedirectToAction(nameof(TeamDetails));
        }

        var isAllTeams = teamId.Value == -1;
        var allTeams = await _db.Teams.OrderBy(t => t.Name).ToListAsync();

        List<TeamGroupReport> groupsToExport = new();

        if (isAllTeams)
        {
            var allAuctionPlayers = await _db.AuctionPlayers
                .Where(ap => ap.TeamId.HasValue)
                .Include(ap => ap.Player)
                .Include(ap => ap.Auction)
                .OrderBy(ap => ap.LotNumber)
                .ThenBy(ap => ap.Player.FullName)
                .ToListAsync();

            foreach (var t in allTeams)
            {
                var players = allAuctionPlayers
                    .Where(ap => ap.TeamId == t.Id)
                    .GroupBy(ap => ap.PlayerId)
                    .Select(g => g.First())
                    .Select(ap => new TeamPlayerItem
                    {
                        Player = ap.Player,
                        Auction = ap.Auction,
                        SoldPrice = ap.SoldPrice,
                        LotNumber = ap.LotNumber
                    })
                    .ToList();

                groupsToExport.Add(new TeamGroupReport { Team = t, Players = players });
            }
        }
        else
        {
            var team = allTeams.FirstOrDefault(t => t.Id == teamId.Value);
            if (team == null) return NotFound("Team not found.");

            var auctionPlayers = await _db.AuctionPlayers
                .Where(ap => ap.TeamId == team.Id)
                .Include(ap => ap.Player)
                .Include(ap => ap.Auction)
                .OrderBy(ap => ap.LotNumber)
                .ThenBy(ap => ap.Player.FullName)
                .ToListAsync();

            var teamPlayers = auctionPlayers
                .GroupBy(ap => ap.PlayerId)
                .Select(g => g.First())
                .Select(ap => new TeamPlayerItem
                {
                    Player = ap.Player,
                    Auction = ap.Auction,
                    SoldPrice = ap.SoldPrice,
                    LotNumber = ap.LotNumber
                })
                .ToList();

            groupsToExport.Add(new TeamGroupReport { Team = team, Players = teamPlayers });
        }

        using var workbook = new XLWorkbook();
        var sheetName = isAllTeams ? "All Teams Player List" : "Team Player List";
        var ws = workbook.Worksheets.Add(sheetName);
        ws.ShowGridLines = true;

        int currentRow = 2;

        // Top Header Box: BCL Team Player List 2026, Session 14
        var titleRange = ws.Range(currentRow, 1, currentRow, 12);
        titleRange.Merge().Value = isAllTeams ? "BCL All Teams Player List 2026, Session 14" : "BCL Team Player List 2026, Session 14";
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(currentRow).Height = 30;
        currentRow += 2;

        foreach (var group in groupsToExport)
        {
            var team = group.Team;
            var teamPlayers = group.Players;

            // Team Name Header Box
            var teamNameRange = ws.Range(currentRow, 1, currentRow, 12);
            teamNameRange.Merge().Value = $"Team Name: {team.Name} ({team.ShortCode})";
            teamNameRange.Style.Font.Bold = true;
            teamNameRange.Style.Font.FontSize = 12;
            teamNameRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            teamNameRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            teamNameRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F7");
            teamNameRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(currentRow).Height = 24;
            currentRow++;

            // Table Header Row: SL | Player Regi. No. | Player Image | Player Name | Role | Batting Style | Bowling Style | Mobile | Jersey Name | Jersey Size | Jersey Number | Note
            string[] headers = [
                "SL", "Player Regi. No.", "Player Image", "Player Name", "Role",
                "Batting Style", "Bowling Style", "Mobile", "Jersey Name", "Jersey Size", "Jersey Number", "Note"
            ];

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Fill.BackgroundColor = XLColor.White;
            }
            ws.Row(currentRow).Height = 22;
            currentRow++;

            if (teamPlayers.Count > 0)
            {
                int sl = 1;
                foreach (var item in teamPlayers)
                {
                    var p = item.Player;
                    var auctionName = item.Auction?.Name ?? "";

                    ws.Cell(currentRow, 1).Value = sl++;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 2).Value = p.Id.ToString();
                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 3).Value = !string.IsNullOrEmpty(p.ProfilePictureUrl) ? "[Photo]" : "-";
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 4).Value = p.FullName;
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Cell(currentRow, 5).Value = p.Role.ToString();
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 6).Value = p.BattingStyle ?? "-";
                    ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 7).Value = p.BowlingStyle ?? "-";
                    ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 8).Value = p.MobileNumber ?? "-";
                    ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 9).Value = p.JerseyName ?? "-";
                    ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 10).Value = p.JerseySize ?? "-";
                    ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 11).Value = p.JerseyNumber ?? "-";
                    ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 12).Value = !string.IsNullOrEmpty(auctionName) ? auctionName : "";
                    ws.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    for (int c = 1; c <= 12; c++)
                    {
                        ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 20;
                    currentRow++;
                }
            }
            else
            {
                // If team currently has no players assigned, output 3 blank rows matching the screenshot template
                for (int emptyRow = 1; emptyRow <= 3; emptyRow++)
                {
                    ws.Cell(currentRow, 1).Value = emptyRow;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    for (int c = 1; c <= 12; c++)
                    {
                        ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 20;
                    currentRow++;
                }
            }

            // Gap between teams if all teams
            currentRow += 2;
        }

        ws.Columns(1, 12).AdjustToContents(8.0, 35.0);
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 24;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 15;
        ws.Column(7).Width = 15;
        ws.Column(8).Width = 16;
        ws.Column(9).Width = 16;
        ws.Column(10).Width = 12;
        ws.Column(11).Width = 15;
        ws.Column(12).Width = 12;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        var downloadName = isAllTeams 
            ? "BCL_All_Teams_Player_List_2026.xlsx" 
            : $"BCL_Team_Details_{(groupsToExport.FirstOrDefault()?.Team.ShortCode ?? "Team")}_2026.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadName);
    }
}
