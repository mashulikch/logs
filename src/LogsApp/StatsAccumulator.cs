using System.Globalization;

namespace LogsApp;

public sealed class LogStatisticsReport
{
    public List<string> Files { get; set; } = new();
    public int TotalRequestsCount { get; set; }
    public ResponseSizeSummary ResponseSizeInBytes { get; set; } = new();
    public List<ResourceEntry> Resources { get; set; } = new();
    public List<ResponseCodeEntry> ResponseCodes { get; set; } = new();
    public List<RequestPerDateEntry>? RequestsPerDate { get; set; } = new();
    public List<string>? UniqueProtocols { get; set; } = new();

    // вспомогательные поля только для markdown/adoc
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? FirstRequestDate { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? LastRequestDate { get; set; }
}

public sealed class ResponseSizeSummary
{
    public int Average { get; set; }
    public int Max { get; set; }
    public int P95 { get; set; }
}

public sealed class ResourceEntry
{
    public string Resource { get; set; } = "";
    public int TotalRequestsCount { get; set; }
}

public sealed class ResponseCodeEntry
{
    public int Code { get; set; }
    public int TotalResponsesCount { get; set; }
}

public sealed class RequestPerDateEntry
{
    public string Date { get; set; } = "";
    public string Weekday { get; set; } = "";
    public int TotalRequestsCount { get; set; }
    public double TotalRequestsPercentage { get; set; }
}

public sealed class StatsAccumulator
{
    private readonly List<long> _sizes = new();
    private readonly Dictionary<string, int> _resourceCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<int, int> _codeCounts = new();
    private readonly Dictionary<DateOnly, int> _requestsPerDate = new();
    private readonly HashSet<string> _protocols = new(StringComparer.Ordinal);

    public List<string> Files { get; } = new();
    public long TotalRequestsCount { get; private set; }
    public long TotalSize { get; private set; }
    public long MaxSize { get; private set; }
    public DateTime? FirstRequest { get; private set; }
    public DateTime? LastRequest { get; private set; }

    public void AddFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return;
        }

        if (Uri.TryCreate(file, UriKind.Absolute, out var uri) &&
             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            Files.Add(file);
        }
        else
        {
            Files.Add(Path.GetFileName(file));
        }
    }

    public void Add(LogEntry entry)
    {
        TotalRequestsCount++;

        _sizes.Add(entry.BodyBytesSent);
        TotalSize += entry.BodyBytesSent;
        if (entry.BodyBytesSent > MaxSize)
        {
            MaxSize = entry.BodyBytesSent;
        }

        if (!string.IsNullOrEmpty(entry.Resource))
        {
            _resourceCounts.TryGetValue(entry.Resource, out var count);
            _resourceCounts[entry.Resource] = count + 1;
        }

        _codeCounts.TryGetValue(entry.StatusCode, out var codeCount);
        _codeCounts[entry.StatusCode] = codeCount + 1;

        var d = DateOnly.FromDateTime(entry.Timestamp);
        _requestsPerDate.TryGetValue(d, out var perDateCount);
        _requestsPerDate[d] = perDateCount + 1;

        if (!string.IsNullOrEmpty(entry.Protocol))
        {
            _protocols.Add(entry.Protocol);
        }

        if (FirstRequest is null || entry.Timestamp < FirstRequest)
        {
            FirstRequest = entry.Timestamp;
        }

        if (LastRequest is null || entry.Timestamp > LastRequest)
        {
            LastRequest = entry.Timestamp;
        }
    }

    public LogStatisticsReport BuildReport()
    {
        var report = new LogStatisticsReport
        {
            Files = Files.ToList(),
            TotalRequestsCount = (int)TotalRequestsCount,
            ResponseSizeInBytes = BuildSizeSummary(),
            Resources = BuildResources(),
            ResponseCodes = BuildCodes(),
            RequestsPerDate = BuildRequestsPerDate(),
            UniqueProtocols = _protocols
                    .OrderBy(p => p == "HTTP/1.1" ? 0 : 1)
                    .ThenBy(p => p, StringComparer.Ordinal)
                    .ToList(),
            FirstRequestDate = FirstRequest,
            LastRequestDate = LastRequest
        };

        return report;
    }

    private ResponseSizeSummary BuildSizeSummary()
    {
        if (_sizes.Count == 0)
        {
            return new ResponseSizeSummary();
        }

        var avg = (double)TotalSize / _sizes.Count;
        var p95 = CalculatePercentile(_sizes, 0.95);

        return new ResponseSizeSummary
        {
            Average = (int)Math.Round(avg, 2, MidpointRounding.AwayFromZero),
            Max = (int)MaxSize,
            P95 = (int)p95
        };
    }

    private static long CalculatePercentile(List<long> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var rank = percentile * (sorted.Length - 1);
        var lowIndex = (int)Math.Floor(rank);
        var highIndex = (int)Math.Ceiling(rank);

        if (lowIndex == highIndex)
        {
            return sorted[lowIndex];
        }

        var weight = rank - lowIndex;
        return (long)Math.Round(sorted[lowIndex] * (1 - weight) + sorted[highIndex] * weight);
    }

    private List<ResourceEntry> BuildResources() =>
        _resourceCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(10)
            .Select(kv => new ResourceEntry
            {
                Resource = kv.Key,
                TotalRequestsCount = kv.Value
            })
            .ToList();

    private List<ResponseCodeEntry> BuildCodes() =>
        _codeCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Select(kv => new ResponseCodeEntry
            {
                Code = kv.Key,
                TotalResponsesCount = kv.Value
            })
            .ToList();

    private List<RequestPerDateEntry>? BuildRequestsPerDate()
    {
        if (_requestsPerDate.Count == 0 || TotalRequestsCount == 0)
        {
            return new List<RequestPerDateEntry>();
        }

        return _requestsPerDate
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var percentage = kv.Value * 100.0 / TotalRequestsCount;
                return new RequestPerDateEntry
                {
                    Date = kv.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Weekday = kv.Key.DayOfWeek.ToString(),
                    TotalRequestsCount = kv.Value,
                    TotalRequestsPercentage = Math.Round(percentage, 2, MidpointRounding.AwayFromZero)
                };
            })
            .ToList();
    }
}
