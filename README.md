# TaskHub

TaskHub is a production-minded ASP.NET Core 8 MVC + API sample for team task management. It combines:

- ASP.NET Core MVC for the web UI
- ASP.NET Core Identity for user registration and sign-in
- Entity Framework Core 8 with SQL Server
- JWT bearer authentication for API access
- A shared service layer so MVC and API enforce the same rules

## Features

- Register, sign in, sign out, and view a profile through the MVC UI
- Register and log in through `/api/auth` to receive a JWT
- Create projects and automatically add the owner as a project member
- Restrict project visibility to members only
- Allow only owners to manage project membership
- Create, edit, assign, and delete tasks within projects
- Restrict task assignment to active users who belong to the project
- Add comments to tasks and view chronological comment history
- Archive projects to stop new task creation
- Use environment-based configuration for SQL Server and JWT settings

## Solution Structure

- `Controllers/` MVC controllers
- `Controllers/Api/` JSON API controllers
- `Data/` `ApplicationDbContext`, EF configurations, design-time factory, migrations
- `Dtos/` API request/response models
- `Models/` domain entities and enums
- `Options/` strongly typed configuration options
- `Services/` business logic and JWT token generation
- `ViewModels/` MVC-specific models
- `Views/` Razor pages for the modern MVC UI

## Prerequisites

- .NET 8 SDK
- SQL Server, SQL Server Express, or LocalDB
- A connection string that points to a writable SQL Server database

## Configuration

Configuration comes from `appsettings.json`, `appsettings.Development.json`, and environment variables.

### Connection string

Default local configuration uses LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TaskHubDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

If you are not using LocalDB, replace it with a SQL Server connection string such as:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=TaskHubDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
}
```

### JWT settings

JWT settings live under the `Jwt` section:

```json
"Jwt": {
  "Issuer": "TaskHub",
  "Audience": "TaskHub.Client",
  "SigningKey": "",
  "ExpirationMinutes": 120
}
```

Important notes:

- `SigningKey` should be provided through configuration for real environments.
- If `SigningKey` is empty in `Development`, the app generates a temporary in-memory signing key so local API testing still works.
- Tokens issued with a generated development key become invalid when the app restarts.

Recommended environment variable overrides:

```bash
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=TaskHubDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
Jwt__Issuer=TaskHub
Jwt__Audience=TaskHub.Client
Jwt__SigningKey=replace-with-a-long-random-secret
Jwt__ExpirationMinutes=120
```

## Database Setup

Apply the included migration:

```bash
dotnet restore
dotnet ef database update
```

If you need to recreate the first migration on your machine:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Running the App

```bash
dotnet run
```

Then open the app in a browser using the URL printed by ASP.NET Core.
If your local HTTPS port differs from `5001`, replace it in the sample `curl` commands below.

## MVC Usage

1. Register a new account.
2. Sign in.
3. Create a project.
4. Open the project and add members by email.
5. Create tasks and assign them to project members.
6. Open a task and post comments.

## API Usage

### Register

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Avery",
    "lastName": "Morgan",
    "email": "avery@example.com",
    "password": "Password123"
  }'
```

### Log in and get a JWT

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "avery@example.com",
    "password": "Password123"
  }'
```

### Call an authenticated endpoint

```bash
curl https://localhost:5001/api/projects \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Authorization Rules

- Anonymous users cannot access project or task management pages.
- Only project members can view project details, tasks, and comments.
- Only project owners can add or remove project members.
- Only project members can create tasks and comments.
- Task assignees must belong to the project and be active.
- Task deletion is limited to the task creator or the project owner.
- Comment editing and deletion is limited to the comment author or the project owner.

## Production Notes

- Secrets are configuration-driven and can be supplied through environment variables or secret stores later.
- The app is structured around a shared service layer, which is appropriate for later containerization or Azure deployment.
- No caching or Redis has been added in this version.
- A `/health` endpoint is included for basic liveness checks.

## Current Environment Note

This repository includes the migration source files needed for EF Core. In the shell used to generate this implementation, the .NET SDK was not available on `PATH`, so build and migration execution could not be run in-session. On a local machine with the .NET 8 SDK installed, use the commands above to restore, build, and apply the database schema.
