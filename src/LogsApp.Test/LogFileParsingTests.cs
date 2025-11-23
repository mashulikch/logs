using System.Text;
using System.Text.Json;
using System.Net;
using LogsApp;
using LogsApp.Test;

namespace Logs.Test;

public class LogFileParsingTests
{
    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LogsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CreateSampleLogFile(string directory, string fileName, IEnumerable<string> lines)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }

    [Fact(DisplayName = "На вход передан валидный локальный log-файл")]
    public void LocalFileProcessingTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log", new[]
            {
                "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /a HTTP/1.1\" 200 100 \"-\" \"UA\"",
                "93.180.71.3 - - [17/May/2015:08:06:32 +0000] \"GET /b HTTP/1.1\" 404 200 \"-\" \"UA\""
            });

            var outputPath = Path.Combine(tempDir, "report.json");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "json"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact(DisplayName = "На вход передан валидный удаленный log-файл")]
    public void RemoteFileProcessingTest()
    {
        var logContent = string.Join("\n", new[]
        {
            "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /a HTTP/1.1\" 200 100 \"-\" \"UA\"",
            "93.180.71.3 - - [17/May/2015:08:06:32 +0000] \"GET /b HTTP/1.1\" 404 200 \"-\" \"UA\""
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, logContent);
        var httpClient = new HttpClient(handler);

        var options = new AppOptions();
        options.Paths.Add("http://unit.test/logs");

        var processor = new LogProcessor(httpClient);
        var stats = processor.Process(options);

        Assert.Equal(2, stats.TotalRequestsCount);
        Assert.Equal(2, stats.ResponseCodes.Count);
        Assert.Contains(stats.ResponseCodes, x => x.Code == 200);
        Assert.Contains(stats.ResponseCodes, x => x.Code == 404);
    }

    [Fact(DisplayName = "На вход передан валидный локальный log-файл, часть строк в котором нужно отфильтровать по --from и --to")]
    public void LocalFileProcessingAndFilteringTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log", new[]
            {
                "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /a HTTP/1.1\" 200 100 \"-\" \"UA\"",
                "93.180.71.3 - - [18/May/2015:08:05:32 +0000] \"GET /b HTTP/1.1\" 200 200 \"-\" \"UA\"",
                "93.180.71.3 - - [19/May/2015:08:05:32 +0000] \"GET /c HTTP/1.1\" 200 300 \"-\" \"UA\""
            });

            var outputPath = Path.Combine(tempDir, "report.json");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "json",
                "--from", "2015-05-18T00:00:00",
                "--to", "2015-05-18T23:59:59"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var json = File.ReadAllText(outputPath);
            var report = JsonSerializer.Deserialize<LogStatisticsReport>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(report);
            Assert.Equal(1, report!.TotalRequestsCount);
            Assert.Single(report.Resources);
            Assert.Equal("/b", report.Resources[0].Resource);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact(DisplayName = "На вход передан локальный log-файл, часть строк в котором не подходит под формат")]
    public void DamagedLocalFileProcessingTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log", new[]
            {
                "this is not a valid nginx log line",
                "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /a HTTP/1.1\" 200 100 \"-\" \"UA\"",
                "garbage line",
                "93.180.71.3 - - [17/May/2015:08:06:32 +0000] \"GET /b HTTP/1.1\" 404 200 \"-\" \"UA\""
            });

            var outputPath = Path.Combine(tempDir, "report.json");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "json"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var json = File.ReadAllText(outputPath);
            var report = JsonSerializer.Deserialize<LogStatisticsReport>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(report);
            Assert.Equal(2, report!.TotalRequestsCount);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}