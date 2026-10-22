using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using LegalManager.Application.Interfaces;
using LegalManager.Application.Services;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.ExternalServices;
using LegalManager.Infrastructure.Persistence;
using LegalManager.Infrastructure.Repositories;
using LegalManager.Presentation;
using LegalManager.Presentation.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IDocumentService>(sp => new DocumentService(
    sp.GetRequiredService<IDocumentRepository>(),
    sp.GetRequiredService<ICaseRepository>(),
    sp.GetRequiredService<IAiSummaryService>(),
    sp.GetRequiredService<ICurrentUser>(),
    builder.Configuration["DocumentStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "DocumentStorage")));

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
        client.BaseAddress = new Uri(llmSettings.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", llmSettings.ApiKey);
        client.DefaultRequestHeaders.Add("x-opencode-session", llmSettings.SessionId);
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
            OnTokenValidated = context =>
            {
                var idValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (!Guid.TryParse(idValue, out var userId))
                {
                    context.Fail("token inválido");
                    return Task.CompletedTask;
                }

                var usersRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = usersRepository.GetById(userId);

                if (user == null || !user.Active)
                {
                    context.Fail("usuario inactivo o no existe");
                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            }
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

SeedInitialAdmin(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseMiddleware<ForbiddenExceptionMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Seeds the first administrator if none exists. Without it there is no way to
// obtain an admin token, because POST /api/user/admin requires the Admin role.
static void SeedInitialAdmin(WebApplication app)
{
    var seed = app.Configuration.GetSection("SeedAdmin");
    var email = seed["Email"];
    var password = seed["Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return;

    using var scope = app.Services.CreateScope();

    var usersRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    if (usersRepository.GetAll().Any(u => u is Admin))
        return;

    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var admin = new Admin(
        seed["FirstName"] ?? "Admin",
        seed["LastName"] ?? "LegalManager",
        seed["Dni"] ?? "00000000",
        email,
        passwordHasher.Hash(password));

    usersRepository.Add(admin);
    usersRepository.Save();

    logger.LogInformation("Admin inicial creado con el email {Email}.", admin.Email);
}