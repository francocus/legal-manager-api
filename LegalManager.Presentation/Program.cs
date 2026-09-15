using System.Text.Json.Serialization;
using LegalManager.Application.Interfaces;
using LegalManager.Application.Services;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using LegalManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LegalManagerDb")
    ?? throw new InvalidOperationException("Falta la connection string 'LegalManagerDb' en appsettings.json.");

builder.Services.AddDbContext<LegalManagerDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserRepository, UsersRepository>();
builder.Services.AddScoped<ICaseRepository, CasesRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentsRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentsRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDocumentService>(sp => new DocumentService(
    sp.GetRequiredService<IDocumentRepository>(),
    sp.GetRequiredService<ICaseRepository>(),
    sp.GetRequiredService<IUserRepository>(),
    builder.Configuration["DocumentStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "DocumentStorage")));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();