using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Gemini;
using DistroCv.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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

// Register application services
builder.Services.AddScoped<DistroCv.Core.Interfaces.IUserService, DistroCv.Infrastructure.Services.UserService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISessionRepository, DistroCv.Infrastructure.Data.SessionRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.ISessionService, DistroCv.Infrastructure.Services.SessionService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IProfileService, DistroCv.Infrastructure.Services.ProfileService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IJobScrapingService, DistroCv.Infrastructure.Services.JobScrapingService>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IJobMatchRepository, DistroCv.Infrastructure.Data.JobMatchRepository>();
builder.Services.AddScoped<DistroCv.Core.Interfaces.IMatchingService, DistroCv.Infrastructure.Services.MatchingService>();

// Configure Playwright settings
builder.Services.Configure<DistroCv.Core.DTOs.PlaywrightSettings>(
    builder.Configuration.GetSection("Playwright"));

// Register background services
builder.Services.AddHostedService<DistroCv.Api.BackgroundServices.SessionCleanupService>();

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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DistroCvDbContext>("database");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionTracking(); // Add session tracking middleware

app.MapControllers();
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
