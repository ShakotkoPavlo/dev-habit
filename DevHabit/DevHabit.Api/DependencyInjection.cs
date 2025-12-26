using System.Text;
using Asp.Versioning;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
            .AddMvc();

        applicationBuilder.Services.AddOpenApi();

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
}
