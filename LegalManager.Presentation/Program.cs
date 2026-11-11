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
using LegalManager.Application.Common.Interfaces;
using LegalManager.Application.Auth.Services;
using LegalManager.Application.Users.Services;
using LegalManager.Application.Cases.Services;
using LegalManager.Application.Appointments.Services;
using LegalManager.Application.Documents.Services;
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
            errorNumbersToAdd: new[] { 40613, 40197, 40501, 49918, 49919, 49920 })));

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

// Account emails (confirmation + password reset): options are lenient at boot like the
// LLM options: a clone without Email/Smtp config must still boot (and dotnet ef must
// work); with no Smtp:Host the sender only logs the message instead of sending.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IAccountEmailService, SmtpAccountEmailService>();
builder.Services.AddScoped<AccountConfirmationService>();

// File storage: uses Azure Blob Storage if configured, otherwise falls back to local file system
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

// Real PDF generation (QuestPDF): renders the AI summary and the constancia
// template as actual PDF files instead of plain text stored as .pdf.
builder.Services.AddScoped<IPdfDocumentRenderer, QuestPdfRenderer>();

// External LLM (OpenCode Go, OpenAI-compatible API): options bound from appsettings;
// the ApiKey is supplied only through user-secrets ("Llm:ApiKey"), never hardcoded.
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.Configure<LlmResilienceOptions>(builder.Configuration.GetSection(LlmResilienceOptions.SectionName));

// Llm:ApiKey is NOT validated here: a clone without user-secrets must still boot
// (and dotnet ef must work). The summary service validates it at call time and the
// controller answers 502 with a clear message.
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", llmSettings.ApiKey);
        }
        if (!string.IsNullOrWhiteSpace(llmSettings.SessionId))
        {
            client.DefaultRequestHeaders.Add("x-opencode-session", llmSettings.SessionId);
        }
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LegalManagerApi/1.0");

        // HttpClient.Timeout must stay infinite: it would wrap the whole retry chain.
        // The per-attempt deadline is enforced by the innermost Polly timeout policy.
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    // Outermost to innermost: retry wraps circuit breaker wraps per-attempt timeout,
    // so the breaker counts failures per attempt and each attempt has its own deadline.
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

        // Effective revocation without a refresh token: if the user was removed or
        // deactivated between token issuance and use, the request is rejected with 401
        // in OnTokenValidated.
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

await SeedInitialAdminAsync(app);

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

// Seeds the first administrator if none exists. Without it there is no way to
// obtain an admin token, because POST /api/user/admin requires the Admin role.
static async Task SeedInitialAdminAsync(WebApplication app)
{
    var seed = app.Configuration.GetSection("SeedAdmin");
    var email = seed["Email"];
    var password = seed["Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return;

    using var scope = app.Services.CreateScope();

    var usersRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var users = await usersRepository.GetAllAsync();
    if (users.Any(u => u is Admin))
        return;

    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var admin = new Admin(
        seed["FirstName"] ?? "Admin",
        seed["LastName"] ?? "LegalManager",
        seed["Dni"] ?? "00000000",
        email,
        passwordHasher.Hash(password));

    // The seeded admin must be able to log in on a fresh database without going
    // through the email confirmation flow.
    admin.ConfirmEmail();

    usersRepository.Add(admin);
    await usersRepository.SaveAsync();

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Admin inicial creado con el email {Email}.", admin.Email);
    }
}