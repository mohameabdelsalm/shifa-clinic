using Serilog;
using Shifa.API.Extensions;
using Shifa.API.Middleware;
using Shifa.Clinic;
using Shifa.Identity;
using Shifa.Identity.Services;
using Shifa.Infrastructure;
using Shifa.Infrastructure.Services;
using Shifa.Patients;
using Shifa.Scheduling;
using Shifa.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.AddShifaLogging();
builder.Services.AddShifaApi(builder.Configuration);
builder.Services.AddShifaInfrastructure();

// Modules: one line per module. Each module registers its own DbContext, repos and controllers.
builder.Services.AddTenancyModule();
builder.Services.AddIdentityModule();
builder.Services.AddClinicModule();
builder.Services.AddPatientsModule();
builder.Services.AddSchedulingModule();

var app = builder.Build();

// Explicit command, never run at startup:
//   dotnet run --project src/Shifa.API -- --seed-platform-admin
// Reads PlatformAdmin:FullName, :Email and :Password from configuration (use user-secrets for the password).
if (args.Contains("--seed-platform-admin"))
{
    var section = app.Configuration.GetSection("PlatformAdmin");
    var created = await PlatformAdminSeeder.SeedAsync(app.Services,
        section["FullName"] ?? "Platform Admin",
        section["Email"] ?? throw new InvalidOperationException("PlatformAdmin:Email is not set."),
        section["Password"] ?? throw new InvalidOperationException("PlatformAdmin:Password is not set."));

    Console.WriteLine(created ? "Platform admin created." : "A user with this email already exists; nothing changed.");
    return;
}

// First in the pipeline so it catches errors from everything after it.
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionHandler>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Development only: read the SMS messages the fake sender "sent" (OTP codes), since nothing real is sent locally.
    app.MapGet("/dev/sms", (InMemorySmsOutbox outbox) => outbox.Messages).AllowAnonymous();
}

app.MapControllers();
app.MapShifaHealthChecks();

app.Run();

// Makes the entry point visible to WebApplicationFactory in the integration tests.
public partial class Program
{
}
