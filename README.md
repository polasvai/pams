# Cricket Auction Manager

A .NET 8 MVC application for managing cricket player auctions with SQLite.

## Features

- Player and team CRUD
- Auction configuration with draft/live/paused/completed states
- Operator-controlled bid console
- Minimum increment, budget, self-bid, and sold-player validation
- ASP.NET Core Identity with a seeded administrator
- Seeded example teams and players

## Run locally

```powershell
dotnet run --project POMS.Web
```

The app creates `POMS.Web/App_Data/poms.db` automatically.

Seeded development login:

- Email: `admin@poms.local`
- Password: `Admin@123!`

Change the seeded credentials before deploying anywhere beyond local development.

## Test

```powershell
dotnet test POMS.sln
```
