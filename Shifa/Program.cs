using Core;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Clinical;
using Core.Interfaces.Identity;
using Core.Interfaces.Patients;
using Core.Interfaces.Scheduling;
using Core.Interfaces.Tenancy;
using Core.Interfaces.Common;
using Core.Interfaces.Dashboard;
using Core.Entities.Identity;
using FluentValidation;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Repositories.Billing;
using Infrastructure.Repositories.Clinic;
using Infrastructure.Repositories.Clinical;
using Infrastructure.Repositories.Dashboard;
using Infrastructure.Repositories.Identity;
using Infrastructure.Repositories.Patients;
using Infrastructure.Repositories.Scheduling;
using Infrastructure.Repositories.Tenancy;
using Infrastructure.Services;
using Infrastructure.Services.Billing;
using Infrastructure.Services.Clinic;
using Infrastructure.Services.Identity;
using Infrastructure.Services.Scheduling;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Shifa.Extensions;
using Shifa.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddShifaLogging();
builder.Services.AddShifaApi(builder.Configuration);
builder.Services.AddShifaInfrastructure();

// DbContexts: one per module, each in its own schema with its own migrations history.
builder.Services.AddModuleDbContext<TenancyDbContext>(TenancyDbContext.SchemaName);
builder.Services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.SchemaName);
builder.Services.AddModuleDbContext<ClinicDbContext>(ClinicDbContext.SchemaName);
builder.Services.AddModuleDbContext<PatientsDbContext>(PatientsDbContext.SchemaName);
builder.Services.AddModuleDbContext<SchedulingDbContext>(SchedulingDbContext.SchemaName);
builder.Services.AddModuleDbContext<ClinicalDbContext>(ClinicalDbContext.SchemaName);
builder.Services.AddModuleDbContext<BillingDbContext>(BillingDbContext.SchemaName);

// AutoMapper profiles and FluentValidation validators live in Core.
builder.Services.AddAutoMapper(typeof(LanguageResource).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(LanguageResource).Assembly, includeInternalTypes: true);

// Tenancy
builder.Services.AddScoped<ITenancyApi, TenancyApi>();
builder.Services.AddScoped<IPlanRepo, PlanRepo>();
builder.Services.AddScoped<ITenantRepo, TenantRepo>();

// Identity
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<MembershipAccessCache>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIdentityApi, IdentityApi>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<IRoleRepo, RoleRepo>();
builder.Services.AddScoped<IStaffRepo, StaffRepo>();

// Clinic
builder.Services.AddScoped<IClinicApi, ClinicApi>();
builder.Services.AddScoped<IBranchRepo, BranchRepo>();
builder.Services.AddScoped<ISpecialtyRepo, SpecialtyRepo>();
builder.Services.AddScoped<IDoctorRepo, DoctorRepo>();
builder.Services.AddScoped<IServiceRepo, ServiceRepo>();

// Patients
builder.Services.AddScoped<IPatientsApi, PatientsApi>();
builder.Services.AddScoped<IPatientRepo, PatientRepo>();
builder.Services.AddScoped<IPortalPatientRepo, PortalPatientRepo>();

// Scheduling
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AppointmentRepo>();
builder.Services.AddScoped<IAppointmentRepo>(sp => sp.GetRequiredService<AppointmentRepo>());
builder.Services.AddScoped<IPortalAppointmentRepo, PortalAppointmentRepo>();
builder.Services.AddScoped<ISchedulingApi, SchedulingApi>();

// Clinical
// Billing
builder.Services.AddScoped<BillingNumbers>();
builder.Services.AddScoped<InvoiceIssuer>();
builder.Services.AddScoped<IOnlineRefunds, UnavailableOnlineRefunds>();
builder.Services.AddScoped<IBillingApi, BillingApi>();
builder.Services.AddScoped<ICashierShiftRepo, CashierShiftRepo>();
builder.Services.AddScoped<IInvoiceRepo, InvoiceRepo>();
builder.Services.AddScoped<IPaymentRepo, PaymentRepo>();
builder.Services.AddScoped<IPortalInvoiceRepo, PortalInvoiceRepo>();

// Dashboard
builder.Services.AddScoped<IDashboardRepo, DashboardRepo>();

// Clinical
builder.Services.AddScoped<IClinicalApi, ClinicalApi>();
builder.Services.AddScoped<IVisitRepo, VisitRepo>();
builder.Services.AddScoped<IPatientClinicalRepo, PatientClinicalRepo>();
builder.Services.AddScoped<IPortalPrescriptionRepo, PortalPrescriptionRepo>();

var app = builder.Build();

// Explicit command, never run at startup:
//   dotnet run --project Shifa -- --seed-platform-admin
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

// Kept public so integration tests (WebApplicationFactory<Program>) can be added back later.
public partial class Program
{
}
