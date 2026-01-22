using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Gemini;
using DistroCv.Infrastructure.Gmail;
using DistroCv.Infrastructure.Caching;
using DistroCv.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize model validation error responses
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new
                {
                    Field = e.Key,
                    Errors = e.Value?.Errors.Select(x => x.ErrorMessage).ToArray()
                })
                .ToList();

            var result = new
            {
                Message = "Validation failed",
                Errors = errors
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL with pgvector
builder.Services.AddDbContext<DistroCvDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseVector();
            npgsqlOptions.EnableRetryOnFailure(3);
        });
});

// Configure AWS Services (Cognito, S3, Lambda)
builder.Services.AddAwsServices(builder.Configuration);

// Configure Gemini Services
builder.Services.AddGeminiServices(builder.Configuration);

// Configure Gmail Services
builder.Services.AddGmailServices();

// Configure Caching Services (Task 29.1)
builder.Services.AddCachingServices(builder.Configuration);

// Register application services
builder.Services.AddScoped<DistroCv.Core.Interfaces.IUserRepository, DistroCv.Infrastructure.Data.UserRepository>(); // Task 2.12
builder.Services.AddScoped<DistroCv.Core.Interfaces.IDigitalTwinRepository, DistroCv.Infrastructure.Data.DigitalTwinRepository>(); // Task 2.13
builder.Services.AddScoped<DistroCv.Core.Interfaces.IUserService, DistroCv.Infrastructure.Services.UserService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISessionRepository, DistroCv.Infrastructure.Data.SessionRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISessionService, DistroCv.Infrastructure.Services.SessionService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IProfileService, DistroCv.Infrastructure.Services.ProfileService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IJobScrapingService, DistroCv.Infrastructure.Services.JobScrapingService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IJobMatchRepository, DistroCv.Infrastructure.Data.JobMatchRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IMatchingService, DistroCv.Infrastructure.Services.MatchingService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.INotificationService, DistroCv.Infrastructure.Services.NotificationService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IResumeTailoringService, DistroCv.Infrastructure.Services.ResumeTailoringService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IApplicationDistributionService, DistroCv.Infrastructure.Services.ApplicationDistributionService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IThrottleManager, DistroCv.Infrastructure.Services.ThrottleManager>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IVerifiedCompanyRepository, DistroCv.Infrastructure.Data.VerifiedCompanyRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IVerifiedCompanyService, DistroCv.Infrastructure.Services.VerifiedCompanyService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISkillGapRepository, DistroCv.Infrastructure.Data.SkillGapRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISkillGapService, DistroCv.Infrastructure.Services.SkillGapService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ILinkedInProfileRepository, DistroCv.Infrastructure.Data.LinkedInProfileRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ILinkedInProfileService, DistroCv.Infrastructure.Services.LinkedInProfileService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IJobPostingRepository, DistroCv.Infrastructure.Data.JobPostingRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IFeedbackService, DistroCv.Infrastructure.Services.FeedbackService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IEncryptionService, DistroCv.Infrastructure.Services.EncryptionService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IAuditLogService, DistroCv.Infrastructure.Services.AuditLogService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IConsentService, DistroCv.Infrastructure.Services.ConsentService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IGDPRService, DistroCv.Infrastructure.Services.GDPRService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IApplicationRepository, DistroCv.Infrastructure.Data.ApplicationRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IInterviewPreparationRepository, DistroCv.Infrastructure.Data.InterviewPreparationRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IInterviewCoachService, DistroCv.Infrastructure.Services.InterviewCoachService>();
// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Configure Services
builder.Services.AddScoped<DistroCv.Core.Interfaces.INotificationPublisher, DistroCv.Api.Services.SignalRNotificationPublisher>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IMetricsService, DistroCv.Infrastructure.Services.CloudWatchMetricsService>();

// Configure Playwright settings
builder.Services.Configure<DistroCv.Core.DTOs.PlaywrightSettings>(
    builder.Configuration.GetSection("Playwright"));

// Register background services
builder.Services.AddHostedService<DistroCv.Api.BackgroundServices.SessionCleanupService>();
builder.Services.AddHostedService<DistroCv.Api.BackgroundServices.JobScrapingBackgroundService>();

// Configure Hangfire for background job processing
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    }));

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent workers
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Configure JWT Authentication
// AWS Cognito uses the User Pool as the issuer
var awsRegion = builder.Configuration["AWS:Region"] ?? "eu-west-1";
var cognitoUserPoolId = builder.Configuration["AWS:CognitoUserPoolId"];
var cognitoClientId = builder.Configuration["AWS:CognitoClientId"];

var jwtIssuer = $"https://cognito-idp.{awsRegion}.amazonaws.com/{cognitoUserPoolId}";
var jwtAudience = cognitoClientId;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtIssuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Cognito uses 'client_id' claim for audience
            AudienceValidator = (audiences, securityToken, validationParameters) =>
            {
                // Cognito ID tokens use 'aud' claim, Access tokens use 'client_id'
                var token = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                if (token == null) return false;
                
                var clientId = token.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;
                var aud = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
                
                return clientId == jwtAudience || aud == jwtAudience;
            }
        };
        
        // Map Cognito claims to standard claims
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Add email claim if not present
                    if (!claimsIdentity.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Email))
                    {
                        var emailClaim = claimsIdentity.FindFirst("email");
                        if (emailClaim != null)
                        {
                            claimsIdentity.AddClaim(new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Email, 
                                emailClaim.Value));
                        }
                    }
                    
                    // Add name identifier claim if not present
                    if (!claimsIdentity.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier))
                    {
                        var subClaim = claimsIdentity.FindFirst("sub");
                        if (subClaim != null)
                        {
                            claimsIdentity.AddClaim(new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.NameIdentifier, 
                                subClaim.Value));
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add Anti-forgery services for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DistroCvDbContext>("database");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    
    // Enable Hangfire Dashboard in development
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

app.UseHttpsRedirection();

// Add security middleware (order matters!)
app.UseSecurityHeaders(); // Add security headers first
app.UseRateLimiting(); // Rate limiting before authentication
app.UseResponseTimeTracking(); // Track API response times (Task 29.5)

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// CSRF protection (after authentication, before endpoints)
// Note: JWT-authenticated requests are exempt from CSRF
app.UseCsrfProtection();

app.UseSessionTracking(); // Add session tracking middleware

app.MapControllers();
app.MapHub<DistroCv.Api.Hubs.NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

// Welcome endpoint
app.MapGet("/", () => Results.Ok(new
{
    Name = "DistroCV API",
    Version = "2.0.0",
    Status = "Running",
    Documentation = "/swagger"
}));

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
