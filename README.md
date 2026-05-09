# Food Backend

Single deployable ASP.NET Core backend service for food/meal users, bookings,
coupons, notifications, and email flows.

## Project Layout

- `Backend` - executable ASP.NET Core service.
- `Backend/Context` - EF Core `AppDbContext`.
- `Backend/Controllers` - API endpoints.
- `Backend/Backend.Repository` - repository abstractions and implementation.
- `Backend/Backend.Service` - email and notification services.

## Required Configuration

Set these values with environment variables, user-secrets, or deployment secrets.
Do not commit production secrets to `appsettings.json`.

```text
ConnectionStrings__sqlServerConnStr=Server=...;Database=MealProjectDb;User Id=...;Password=...;TrustServerCertificate=True
Jwt__SigningKey=<long random signing key>
Jwt__Issuer=Food.Backend
Jwt__Audience=Food.Client
Cors__AllowedOrigins__0=http://localhost:4200
EmailSettings__From=no-reply@example.com
EmailSettings__SmtpServer=smtp.example.com
EmailSettings__Port=465
EmailSettings__Username=<smtp-user>
EmailSettings__Password=<smtp-password>
```

Optional runtime controls:

```text
Database__AutoMigrate=false
Swagger__Enabled=false
App__HttpsRedirectionEnabled=true
```

## Local Run

```powershell
dotnet restore Backend.sln
dotnet build Backend.sln
dotnet run --project Backend/Backend.csproj
```

Health check:

```text
/health
```

## Docker

```powershell
docker build -t food-backend:local .
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_URLS=http://+:8080 `
  -e App__HttpsRedirectionEnabled=false `
  -e ConnectionStrings__sqlServerConnStr="<connection-string>" `
  -e Jwt__SigningKey="<long-random-signing-key>" `
  food-backend:local
```

## Helm

The Helm chart deploys the same single backend service.

```powershell
helm template food-backend charts/food-backend
helm upgrade --install food-backend charts/food-backend
```

Provide production values through `secretEnv.stringData`, `secretEnv.existingSecret`,
or your platform's secret management.
