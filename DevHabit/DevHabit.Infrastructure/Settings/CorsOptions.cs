namespace DevHabit.Infrastructure.Settings;

public class CorsOptions
{
    public const string PolicyName = "DevHabitCorsPolicy";
    public const string Section = "Cors";

    public required string[] AllowedOrigins { get; init; }
}
