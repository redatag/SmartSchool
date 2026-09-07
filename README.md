# SmartSchool

SmartSchool is a .NET 10 modular monolith. The current milestone contains only
the Shared Kernel and the Identity bounded context.

## Run locally

1. Set `ConnectionStrings__Database` to a SQL Server connection string.
2. Replace the development JWT key in configuration with a secret of at least 32 characters.
3. Run `dotnet restore`, `dotnet test`, then `dotnet run --project src/SmartSchool.Api`.

Apply the Identity migration with:

```powershell
dotnet tool restore
dotnet ef database update --project src/Modules/Identity/SmartSchool.Modules.Identity.Infrastructure --startup-project src/SmartSchool.Api
```

The Identity module owns the `identity` SQL schema and its migration history is
stored in `identity.__EFMigrationsHistory`.
