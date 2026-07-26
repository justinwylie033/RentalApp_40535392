using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalApp.Api.Endpoints;
using RentalApp.Api.Services;
using RentalApp.Database.Data;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.States;
using RentalApp.Migrations;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

// Presentation point: Program.cs is the API composition root. It wires concrete
// implementations to interfaces, while business logic remains in testable services.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
    connectionString,
    postgres =>
    {
        postgres.UseNetTopologySuite();
        postgres.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.FullName);
        postgres.EnableRetryOnFailure(3);
    }));

builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IRentalRepository, RentalRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IItemApplicationService, ItemApplicationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IRentalWorkflowService, RentalWorkflowService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IRentalState, RequestedState>();
builder.Services.AddSingleton<IRentalState, ApprovedState>();
builder.Services.AddSingleton<IRentalState, RejectedState>();
builder.Services.AddSingleton<IRentalState, CancelledState>();
builder.Services.AddSingleton<IRentalState, OutForRentState>();
builder.Services.AddSingleton<IRentalState, OverdueState>();
builder.Services.AddSingleton<IRentalState, ReturnedState>();
builder.Services.AddSingleton<IRentalState, CompletedState>();
builder.Services.AddSingleton<RentalStateMachine>();
builder.Services.AddHostedService<OverdueRentalWorker>();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options => options.Secret.Length >= 32, "JWT secret must contain at least 32 characters.")
    .ValidateOnStart();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    });
builder.Services.AddAuthorization();

var app = builder.Build();
// Presentation point: exception mapping runs before the endpoint pipeline so
// service-layer exceptions become consistent HTTP Problem Details responses.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
// Presentation point: only health and authentication entry points are anonymous;
// each feature endpoint group applies RequireAuthorization at its boundary.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapAuthEndpoints();
app.MapItemEndpoints();
app.MapRentalEndpoints();
app.MapReviewEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    // Presentation point: schema upgrades happen before traffic is accepted. This
    // makes Docker deployment repeatable and keeps database state aligned with code.
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var migrations = database.Database.GetMigrations().ToArray();
    if (migrations.Length > 0)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseBootstrap");
        await DatabaseBootstrapper.PrepareLegacySchemaAsync(database, logger);
        await database.Database.MigrateAsync();
    }
    else
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseBootstrap");
        logger.LogWarning(
            "No EF Core migrations were discovered; creating the database schema from the current model.");
        await database.Database.EnsureCreatedAsync();
    }

    await DatabaseSeeder.SeedAsync(database);
}

await app.RunAsync();

public partial class Program;
