PongRank
========


## Deploy

```sh
cp .example.env .env
cp src/PongRank.WebApi/appsettings.json src/PongRank.WebApi/appsettings.Release.json

docker-compose up -d --build
```

## Database

Use `docker compose up -d --build`.


## EF Migrations

Migrations will run at startup of application.

```ps1
cd src/PongRank.DataAccess
dotnet ef database update

# Install
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef

# Create
dotnet ef migrations add InitialCreate

# Delete
dotnet ef migrations remove
dotnet ef database drop -f
```

## Sync Progress

```sql
SELECT "Competition", "Year", "CategoryName", "SyncCompleted", COUNT(0)
FROM "Clubs"
GROUP BY "Competition", "Year", "CategoryName", "SyncCompleted"
ORDER BY "Competition", "Year", "CategoryName", "SyncCompleted"
```
