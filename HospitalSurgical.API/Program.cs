using HospitalSurgical.Application.Services;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using HospitalSurgical.Infrastructure.Interceptors;
using HospitalSurgical.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Audit Interceptor (Step 5 — Shadow Properties)
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<AuditInterceptor>();

// ── 2. DbContext with interceptor
builder.Services.AddDbContext<SurgicalDbContext>((sp, options) =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("HospitalSurgical.Infrastructure"));
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

// ── 3. Repositories & Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ISurgeryRepository, SurgeryRepository>();
builder.Services.AddScoped<IOperatingRoomRepository, OperatingRoomRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IStaffService, StaffService>();

// ── 4. Application Services
builder.Services.AddScoped<ISurgeryService, SurgeryService>();

// ── 5. Controllers
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hospital Surgical Management API",
        Version = "v1",
        Description = "Operating rooms, surgeries, and surgical staff management."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Surgical API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SurgicalDbContext>();
    db.Database.Migrate();
}

app.Run();