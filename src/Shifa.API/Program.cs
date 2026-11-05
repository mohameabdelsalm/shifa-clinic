using Shifa.Clinic;

var builder = WebApplication.CreateBuilder(args);

// Modules: one line per module. Each module registers its own services.
builder.Services.AddClinicModule(builder.Configuration);

var app = builder.Build();

app.MapControllers();

app.Run();
