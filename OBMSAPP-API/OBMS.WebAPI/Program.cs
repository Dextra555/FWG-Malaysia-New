using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Repositories.Implementation;
using OBMS.WebAPI.Repositories.Interface;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Environment Configuration
var environment = builder.Environment;

builder.Configuration
    .AddJsonFile($"appsettings.{environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true);

// JWT Key
var key = Encoding.ASCII.GetBytes("obms-authentication");

// Database Connection
builder.Services.AddDbContext<OBMSDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("obms"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// Controllers
builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddMvc().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddScoped<IRegisterRepository, RegisterRepository>();
builder.Services.AddScoped<IMasterRepository, MasterRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IAgreementRepository, AgreementRepository>();
// Part 7: Notification + Approval must be registered before InventoryRepository
// because InventoryRepository optionally depends on them.
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
builder.Services.AddScoped<ISalaryProcess, SalaryProcess>();
builder.Services.AddScoped<IAccountingRepository, AccountingRepository>();
builder.Services.AddScoped<ICreditDebitNoteRepository, CreditDebitNoteRepository>();

// JWT Authentication
// SignalR sends the token as a query-string parameter (?access_token=…)
// because WebSockets cannot set Authorization headers.
// We read it from the query string and forward it as the bearer token.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "yourapi.com",
        ValidAudience = "yourapi.com",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    // Allow the token to come from the query string for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // Only apply the query-string token to the SignalR hub endpoint
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// SignalR — real-time notification delivery
builder.Services.AddSignalR();

var app = builder.Build();

// Swagger - ALL Environments
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS
app.UseHttpsRedirection();

// CORS
// SignalR with credentials requires explicit origins — AllowAnyOrigin is incompatible
// with AllowCredentials (WebSocket auth). We allow all known OBMS origins.
app.UseCors(policy =>
    policy.WithOrigins(
            "http://localhost:4200",          // Angular dev server
            "http://localhost:5002",          // API self (Swagger)
            "http://124.217.236.37:64499",    // FWG production API origin
            "http://124.217.236.37:8227",     // UAT server
            "http://124.217.236.37:8484",     // IND server
            "http://124.217.236.37"           // bare host fallback
          )
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());               // required for SignalR auth

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR hub endpoint
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();