using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middleware;
using DevHabit.Api.Providers;
using DevHabit.Application.Services;
using DevHabit.Contracts;
using DevHabit.Contracts.Habits.Requests;
using DevHabit.Infrastructure.Database;
using DevHabit.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Refit;

namespace DevHabit.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services
            .AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
            })
            .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
            .AddXmlSerializerFormatters();

        applicationBuilder.Services.Configure<MvcOptions>(opt =>
        {
            NewtonsoftJsonOutputFormatter jsonOutputFormatter = opt.OutputFormatters
                .OfType<NewtonsoftJsonOutputFormatter>()
                .FirstOrDefault()!;

            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.JsonV1);
            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.JsonV2);
            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJson);
            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJsonV1);
            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJsonV2);
        });

        applicationBuilder.Services
            .AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionSelector = new DefaultApiVersionSelector(opt);

                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new MediaTypeApiVersionReader(),
                    new MediaTypeApiVersionReaderBuilder()
                        .Template("application/vnd.dev-habit.hateoas.{version}+json")
                        .Build());
            })
            .AddMvc()
            .AddApiExplorer();

        //applicationBuilder.Services.AddOpenApi();

        applicationBuilder.Services.AddSwaggerGen();
        applicationBuilder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
        applicationBuilder.Services.ConfigureOptions<ConfigureSwaggerUIOptions>();

        applicationBuilder.Services.AddResponseCaching();

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddProblemDetails(config =>
            config.CustomizeProblemDetails = context => context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier));

        applicationBuilder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        applicationBuilder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(
                    applicationBuilder.Configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, DatabaseConstants.ApplicationSchema))
                .UseSnakeCaseNamingConvention());

        applicationBuilder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
            options
                .UseNpgsql(
                    applicationBuilder.Configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, DatabaseConstants.IdentitySchema))
                .UseSnakeCaseNamingConvention());

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(applicationBuilder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        applicationBuilder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddValidatorsFromAssemblyContaining<CreateHabitRequestValidator>();

        applicationBuilder.Services.AddHttpContextAccessor();

        applicationBuilder.Services.AddTransient<DataShapingService>();
        applicationBuilder.Services.AddTransient<LinkService>();

        applicationBuilder.Services.AddTransient<TokenProvider>();
        applicationBuilder.Services.AddMemoryCache();
        applicationBuilder.Services.AddScoped<UserContext>();

        applicationBuilder.Services.AddScoped<GitHubAccessTokenService>();
        applicationBuilder.Services.AddTransient<RefitGitHubService>();
        applicationBuilder.Services.AddTransient<EncryptionService>();

        applicationBuilder.Services.AddHttpClient("github")
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("https://api.github.com");

                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DevHabit", "1.0"));

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            });

        applicationBuilder.Services.AddRefitClient<IIGitHubApi>(new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer()
            })
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://api.github.com"))
            .AddResilienceHandler("custom", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(5));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(500),
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    FailureRatio = 0.9,
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(5),
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(1));
            });

        applicationBuilder.Services.Configure<EncryptionOptions>(applicationBuilder.Configuration.GetSection("Encryption"));

        applicationBuilder.Services.AddSingleton<ETagMiddleware.InMemoryETagStore>();

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder applicationBuilder)
    {
        CorsOptions corsOptions = applicationBuilder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()!;

        applicationBuilder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                policy
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddAuthenticationServices(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services
            .AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        applicationBuilder.Services.Configure<JwtAuthOptions>(applicationBuilder.Configuration.GetSection("Jwt"));

        JwtAuthOptions jwtAuthOptions = applicationBuilder.Configuration.GetSection("Jwt").Get<JwtAuthOptions>();

        applicationBuilder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtAuthOptions!.Issuer,
                    ValidAudience = jwtAuthOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key))
                };
            });

        applicationBuilder.Services.AddAuthorization();

        return applicationBuilder;
    }

    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = $"{retryAfter.TotalSeconds}";

                    ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ProblemDetailsFactory>();
                    Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = problemDetailsFactory
                        .CreateProblemDetails(
                            context.HttpContext,
                            StatusCodes.Status429TooManyRequests,
                            "Too Many Requests",
                            detail: $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds."
                        );

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: token);
                }
            };

            options.AddPolicy("default", httpContext =>
            {
                string identityId = httpContext.User.GetIdentityId() ?? string.Empty;

                if (!string.IsNullOrEmpty(identityId))
                {
                    return RateLimitPartition.GetTokenBucketLimiter(
                        identityId,
                        _ =>
                            new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 100,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 5,
                                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                                TokensPerPeriod = 25
                            });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    "anonymous",
                    _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1)
                        });
            });
        });

        return builder;
    }
}
