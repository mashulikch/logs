using System.Text.Json;

namespace LogsApp;

public sealed class JsonReportFormatter : IReportFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Format(LogStatisticsReport stats) =>
        JsonSerializer.Serialize(stats, Options);
}