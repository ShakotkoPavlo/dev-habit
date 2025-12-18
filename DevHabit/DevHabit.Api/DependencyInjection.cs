using DevHabit.Api.Middleware;
using DevHabit.Application.Services;
using DevHabit.Contracts.Habits.Requests;
using DevHabit.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevHabit.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder applicationBuilder)
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

            jsonOutputFormatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJson);
        });

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

        return applicationBuilder;
    }
}
