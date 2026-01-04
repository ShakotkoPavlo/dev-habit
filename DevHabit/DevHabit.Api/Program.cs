using DevHabit.Api;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddDatabase()
    .AddOpenTelemetry()
    .AddApplicationServices()
    .AddAuthenticationServices()
    .AddCorsPolicy()
    .AddRateLimiting();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await app.ApplyMigrationsAsync();

    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors("DevHabitCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync();
