using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using Kickstarter.Api.Authorization;
using Kickstarter.Api.Startup;
using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Repositories;
using Kickstarter.Application.Users;
using Kickstarter.Application.Validation.Users;
using Kickstarter.Domain.Entities;
using Kickstarter.Infrastructure.Data;
using Kickstarter.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);

builder
    .Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    // Include only the canonical versioned routes in each version-specific document.
    options.DocInclusionPredicate(
        (documentName, apiDescription) =>
            apiDescription.GroupName == documentName
            && apiDescription.RelativePath?.StartsWith("api/v", StringComparison.OrdinalIgnoreCase) == true
    );

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "opaque access token",
            Description = "ASP.NET Core Identity bearer access token",
        }
    );
    options.OperationFilter<AuthorizeOperationFilter>();
});
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSerilog(
    (_, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

        var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = endpoint.ToString();
                options.Protocol = OtlpProtocol.HttpProtobuf;

                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "repodog-dotnet-kickstarter",
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                };
            });
        }
    }
);

builder.Services.Configure<RequestLogContextOptions>(builder.Configuration.GetSection("RequestLogging"));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder
    .Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
});

builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
builder.Services.AddScoped<CurrentUserResponseFactory>();
builder.Services.AddFeatureManagement();

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false))
    );

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false))
);

builder.Services.Configure<AccountRecoveryOptions>(
    builder.Configuration.GetSection(AccountRecoveryOptions.SectionName)
);
builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection(SmtpEmailOptions.SectionName));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(1)
);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDb"))
);

builder.Services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
var app = builder.Build();

app.UseRouting();
app.UseMiddleware<RequestLogContextMiddleware>();
app.UseSerilogRequestLogging();

app.Use(
    async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (KeyNotFoundException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message }, context.RequestAborted);
        }
        catch (ArgumentException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message }, context.RequestAborted);
        }
        catch (InvalidOperationException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message }, context.RequestAborted);
        }
        catch (DbUpdateException) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;

            await context.Response.WriteAsJsonAsync(
                new { error = "The request conflicts with existing data." },
                context.RequestAborted
            );
        }
    }
);

app.UseAuthentication();
app.UseAuthorization();

// Create the starter schema on first run. Replace this with EF migrations
// once the domain model begins to evolve.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await UserBootstrapper.EnsureUserAsync(scope.ServiceProvider, builder.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.EnableSwaggerDocumentUrlsEndpoint();

        var versionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in versionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", description.GroupName.ToUpperInvariant());
        }
    });
}

app.MapControllers();

app.Run();
