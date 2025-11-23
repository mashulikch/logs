using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;

namespace LogsApp;

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Resource { get; init; } = "";
    public int StatusCode { get; init; }
    public long BodyBytesSent { get; init; }
    public string Protocol { get; init; } = "";
}

public static class NginxLogParser
{
    private static readonly Regex LineRegex = new(
        @"^(?<remote_addr>\S+)\s+-\s+(?<remote_user>\S+)\s+\[(?<time_local>[^\]]+)\]\s+""(?<request>[^""]*)""\s+(?<status>\d{3})\s+(?<body_bytes_sent>\d+)\s+""(?<http_referer>[^""]*)""\s+""(?<user_agent>[^""]*)""\s*$",
        RegexOptions.Compiled);

    public static bool TryParse(string line, out LogEntry? entry)
    {
        entry = null;
        var match = LineRegex.Match(line);
        if (!match.Success)
        {
            Log.Warning("Line does not match expected NGINX format and will be skipped: {Line}", line);
            return false;
        }

        var timeLocal = match.Groups["time_local"].Value;

        var spaceIndex = timeLocal.IndexOf(' ');
        var datePart = spaceIndex >= 0 ? timeLocal[..spaceIndex] : timeLocal;

        var dateFormats = new[]
        {
            "dd/MMM/yyyy:HH:mm:ss",
            "d/MMM/yyyy:HH:mm:ss"
        };

        if (!DateTime.TryParseExact(
                datePart,
                dateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var timestamp))
        {
            Log.Warning("Cannot parse date '{Date}' in line: {Line}", timeLocal, line);
            return false;
        }

        var request = match.Groups["request"].Value;
        var requestParts = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (requestParts.Length < 2)
        {
            Log.Warning("Request part '{Request}' cannot be parsed in line: {Line}", request, line);
            return false;
        }

        var resource = requestParts[1];
        var protocol = requestParts.Length >= 3 ? requestParts[2] : "";

        if (!int.TryParse(match.Groups["status"].Value, out var status))
        {
            Log.Warning("Cannot parse status code in line: {Line}", line);
            return false;
        }

        if (!long.TryParse(match.Groups["body_bytes_sent"].Value, out var bytes))
        {
            Log.Warning("Cannot parse body_bytes_sent in line: {Line}", line);
            return false;
        }

        entry = new LogEntry
        {
            Timestamp = timestamp,
            Resource = resource,
            StatusCode = status,
            BodyBytesSent = bytes,
            Protocol = protocol
        };

        return true;
    }
}
