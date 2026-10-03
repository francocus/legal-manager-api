using System.Text;
using System.Text.Json.Serialization;
using LegalManager.Application.Interfaces;
using LegalManager.Application.Services;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.ExternalServices;
using LegalManager.Infrastructure.Persistence;
using LegalManager.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LegalManager.Presentation;

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IDocumentService>(sp => new DocumentService(
    sp.GetRequiredService<IDocumentRepository>(),
    sp.GetRequiredService<ICaseRepository>(),
    sp.GetRequiredService<IUserRepository>(),
    builder.Configuration["DocumentStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "DocumentStorage")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    throw new InvalidOperationException("Falta la clave 'Jwt:Key' en appsettings.json.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminsOnly, policy => policy.RequireRole(nameof(Admin)));

    options.AddPolicy(Policies.AdminOrLawyer, policy => policy.RequireRole(nameof(Admin), nameof(Lawyer)));

    options.AddPolicy(Policies.AllRoles, policy => policy.RequireRole(nameof(Admin), nameof(Lawyer), nameof(Client)));
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();