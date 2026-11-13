using LegalManager.Infrastructure.Storage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using LegalManager.Application.Auth;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Trash.Interfaces;
using LegalManager.Application.Common.Interfaces;
using LegalManager.Application.Auth.Services;
using LegalManager.Application.Users.Services;
using LegalManager.Application.Cases.Services;
using LegalManager.Application.Appointments.Services;
using LegalManager.Application.Documents.Services;
using LegalManager.Application.Trash.Services;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Email;
using LegalManager.Infrastructure.Auth;
using LegalManager.Infrastructure.AI;
using LegalManager.Infrastructure.Persistence;
using LegalManager.Infrastructure.Pdf;
using LegalManager.Infrastructure.Repositories;
using LegalManager.Presentation;
using LegalManager.Presentation.Auth;
using LegalManager.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LegalManagerDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Falta la connection string 'LegalManagerDb' en appsettings.json o en el entorno.");

builder.Services.AddDbContext<LegalManagerDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(20),
            errorNumbersToAdd: [40613, 40197, 40501, 49918, 49919, 49920])));

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IAccountEmailService, SmtpAccountEmailService>();
builder.Services.AddScoped<AccountConfirmationService>();

var azureStorageConn = builder.Configuration["AzureStorage:ConnectionString"];
var azureContainer = builder.Configuration["AzureStorage:ContainerName"] ?? "documents";

if (!string.IsNullOrWhiteSpace(azureStorageConn))
{
    builder.Services.AddSingleton<IFileStorage>(new AzureBlobFileStorage(azureStorageConn, azureContainer));
}
else
{
    var localPath = builder.Configuration["DocumentStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "DocumentStorage");
    builder.Services.AddSingleton<IFileStorage>(new LocalFileSystemStorage(localPath));
}

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ITrashService, TrashService>();

builder.Services.AddScoped<IPdfDocumentRenderer, QuestPdfRenderer>();

builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.Configure<LlmResilienceOptions>(builder.Configuration.GetSection(LlmResilienceOptions.SectionName));

var llmSettings = builder.Configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Llm' en appsettings.json.");

var llmResilience = builder.Configuration.GetSection(LlmResilienceOptions.SectionName).Get<LlmResilienceOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Llm:Resilience' en appsettings.json.");

builder.Services
    .AddHttpClient<IAiSummaryService, OpenCodeAiSummaryService>(client =>
    {
        if (!string.IsNullOrWhiteSpace(llmSettings.BaseUrl))
        {
            client.BaseAddress = new Uri(llmSettings.BaseUrl);
        }
        if (!string.IsNullOrWhiteSpace(llmSettings.ApiKey))
        {
            var cleanKey = llmSettings.ApiKey.Trim().Trim('<', '>');
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cleanKey);
        }
        if (!string.IsNullOrWhiteSpace(llmSettings.SessionId))
        {
            client.DefaultRequestHeaders.Add("x-opencode-session", llmSettings.SessionId);
        }
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LegalManagerApi/1.0");

        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddPolicyHandler(LlmPolicies.GetRetryPolicy(llmResilience))
    .AddPolicyHandler(LlmPolicies.GetCircuitBreakerPolicy(llmResilience))
    .AddPolicyHandler(LlmPolicies.GetTimeoutPolicy(llmResilience));

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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (!Guid.TryParse(idValue, out var userId))
                {
                    context.Fail("token inválido");
                    return;
                }

                var usersRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await usersRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);

                if (user == null || !user.Active)
                {
                    context.Fail("usuario inactivo o no existe");
                    return;
                }
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminsOnly, policy => policy.RequireRole(nameof(Admin)))
    .AddPolicy(Policies.AdminOrLawyer, policy => policy.RequireRole(nameof(Admin), nameof(Lawyer)))
    .AddPolicy(Policies.AllRoles, policy => policy.RequireRole(nameof(Admin), nameof(Lawyer), nameof(Client)));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseMiddleware<ForbiddenExceptionMiddleware>();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();