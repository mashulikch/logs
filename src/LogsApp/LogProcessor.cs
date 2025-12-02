using System.Net;
using Serilog;

namespace LogsApp;

public sealed class LogProcessor
{
    private readonly HttpClient? _httpClient;

    public LogProcessor(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
    }

    public LogStatisticsReport Process(AppOptions options)
    {
        var accumulator = new StatsAccumulator();

        foreach (var path in options.Paths)
        {
            if (IsRemote(path))
            {
                ProcessRemote(path, options, accumulator);
            }
            else
            {
                ProcessLocal(path, options, accumulator);
            }
        }

        return accumulator.BuildReport();
    }

    private static bool IsRemote(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private void ProcessLocal(string path, AppOptions options, StatsAccumulator acc)
    {
        if (HasWildcards(path))
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir))
            {
                dir = Directory.GetCurrentDirectory();
            }

            var pattern = Path.GetFileName(path);

            foreach (var file in Directory.EnumerateFiles(dir, pattern))
            {
                ProcessLocalFile(file, options, acc);
            }
        }
        else
        {
            ProcessLocalFile(path, options, acc);
        }
    }

    private void ProcessLocalFile(string filePath, AppOptions options, StatsAccumulator acc)
    {
        Log.Information("Processing local file {File}", filePath);
        acc.AddFile(filePath);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            ProcessLine(line, options, acc);
        }
    }

    private void ProcessRemote(string url, AppOptions options, StatsAccumulator acc)
    {
        Log.Information("Processing remote file {Url}", url);
        acc.AddFile(url);

        if (_httpClient is null)
        {
            using var client = new HttpClient();
            ProcessRemoteCore(client, url, options, acc);
        }
        else
        {
            ProcessRemoteCore(_httpClient, url, options, acc);
        }
    }

    private static void ProcessRemoteCore(HttpClient client, string url, AppOptions options, StatsAccumulator acc)
    {
        using var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new UsageException($"Remote file '{url}' not found (404)");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UsageException($"Remote file '{url}' returned status {(int)response.StatusCode}");
        }

        using var stream = response.Content.ReadAsStreamAsync().Result;
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            ProcessLine(line, options, acc);
        }
    }

    private static void ProcessLine(string line, AppOptions options, StatsAccumulator acc)
    {
        if (!NginxLogParser.TryParse(line, out var entry) || entry is null)
        {
            return;
        }

        if (!IsWithinRange(entry.Timestamp, options.From, options.To))
        {
            return;
        }

        acc.Add(entry);
    }

    private static bool IsWithinRange(DateTime ts, DateTime? from, DateTime? to)
    {
        var date = DateOnly.FromDateTime(ts);

        if (from is not null)
        {
            var fromDate = DateOnly.FromDateTime(from.Value);
            if (date < fromDate)
            {
                return false;
            }
        }

        if (to is not null)
        {
            var toDate = DateOnly.FromDateTime(to.Value);
            if (date > toDate)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasWildcards(string path) =>
        path.IndexOfAny(new[] { '*', '?' }) >= 0;
}
