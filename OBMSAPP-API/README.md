# OBMS Web API

## Getting started

Install the .NET 8 SDK and configure a reachable SQL Server connection in
`OBMS.WebAPI/appsettings.Development.json` (or `appsettings.json` for Production).

From this directory, restore and run the API project:

```powershell
dotnet restore .\OBMS.WebAPI\OBMS.WebAPI.csproj
dotnet run --project .\OBMS.WebAPI\OBMS.WebAPI.csproj
```

The API listens at `http://localhost:5002`; Swagger UI is available at
`http://localhost:5002/swagger`.

## Build

```powershell
dotnet build .\OBMS.WebAPI.sln
```

`website.publishproj` is a legacy Visual Studio Web Site publishing project and
requires Visual Studio Web publishing targets. It is not the ASP.NET Core API
project and should not be passed to `dotnet run`.
