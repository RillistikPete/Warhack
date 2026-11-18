# WarHack

A quick-reference site for World of Warcraft characters. Look up a character and get a short, accurate summary of their stats, gear, and enhancements on one page.

## Tech stack

- **.NET 10** / C# / ASP.NET Core MVC
- **Entity Framework Core** with **PostgreSQL**
- **ASP.NET Core Identity** for accounts
- **Blizzard Battle.net API** via OAuth2 client credentials
- **Docker** for the local database

## Design highlights

- **Typed HttpClient** for the Blizzard API, registered through dependency injection.
- **EF Core projections** into view models instead of loading full entities, so queries pull only the columns a page needs.
- **Value objects and richer domain models** to keep business rules in the domain instead of spread across services.

## Running locally

### Prerequisites

- .NET 10 SDK
- Docker
- A Blizzard developer client ID and secret ([develop.battle.net](https://develop.battle.net))

### Steps

1. Start Postgres (runs on host port 5433):

   ```bash
   docker compose up -d db
   ```

2. Add your Blizzard credentials:

   ```bash
   cd src/<WebProject>
   dotnet user-secrets set "Blizzard:ClientId" "<your-client-id>"
   dotnet user-secrets set "Blizzard:ClientSecret" "<your-client-secret>"
   ```

3. Apply migrations and run:

   ```bash
   dotnet ef database update
   dotnet run
   ```

## Status

In active development.

## Author

G. Forrest · [blog.fpkfaculty.com](https://blog.fpkfaculty.com)