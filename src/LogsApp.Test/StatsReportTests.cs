using System.Text;
using System.Text.Json;
using LogsApp;

namespace Logs.Test;

public class StatsReportTests
{
    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LogsReports_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CreateSampleLogFile(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        var lines = new[]
        {
            "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /index HTTP/1.1\" 200 100 \"-\" \"UA\"",
            "93.180.71.3 - - [17/May/2015:09:05:32 +0000] \"GET /about HTTP/1.1\" 404 200 \"-\" \"UA\""
        };
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }

    [Fact(DisplayName = "Сохранение статистики в формате JSON")]
    public void JsonTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log");
            var outputPath = Path.Combine(tempDir, "report.json");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "json"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var text = File.ReadAllText(outputPath);
            var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("files", out _));
            Assert.True(root.TryGetProperty("totalRequestsCount", out _));
            Assert.True(root.TryGetProperty("responseSizeInBytes", out _));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact(DisplayName = "Сохранение статистики в формате MARKDOWN")]
    public void MarkdownTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log");
            var outputPath = Path.Combine(tempDir, "report.md");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "markdown"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var text = File.ReadAllText(outputPath);

            Assert.Contains("#### Общая информация", text);
            Assert.Contains("| Файл(-ы)", text);
            Assert.Contains("#### Коды ответа", text);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact(DisplayName = "Сохранение статистики в формате ADOC")]
    public void AdocTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = CreateSampleLogFile(tempDir, "access.log");
            var outputPath = Path.Combine(tempDir, "report.ad");

            var exitCode = Program.Run(new[]
            {
                "--path", logPath,
                "--output", outputPath,
                "--format", "adoc"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var text = File.ReadAllText(outputPath);

            Assert.Contains("=== Общая информация", text);
            Assert.Contains("| Файл(-ы)", text);
            Assert.Contains("=== Коды ответа", text);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}