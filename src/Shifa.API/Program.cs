using Serilog;
using Shifa.API.Extensions;
using Shifa.API.Middleware;
using Shifa.Clinic;
using Shifa.Infrastructure;
using Shifa.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.AddShifaLogging();
builder.Services.AddShifaApi();
builder.Services.AddShifaInfrastructure();

// Modules: one line per module. Each module registers its own DbContext, repos and controllers.
builder.Services.AddTenancyModule();
builder.Services.AddClinicModule();

var app = builder.Build();

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

app.UseAuthentication();
app.UseMiddleware<TenantResolutionHandler>();
app.UseAuthorization();

app.MapControllers();
app.MapShifaHealthChecks();

app.Run();
