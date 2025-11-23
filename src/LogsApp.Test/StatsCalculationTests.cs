using System.Text;
using System.Text.Json;
using LogsApp;

namespace Logs.Test;

public class StatsCalculationTests
{
    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LogsStats_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact(DisplayName = "Расчет статистики на основании локального log-файла")]
    public void HappyPathTest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var logPath = Path.Combine(tempDir, "access.log");

            // 4 строки с известными размерами, ресурсами и кодами
            var lines = new[]
            {
                "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /a HTTP/1.1\" 200 100 \"-\" \"UA\"",
                "93.180.71.3 - - [17/May/2015:09:05:32 +0000] \"GET /a HTTP/1.1\" 200 300 \"-\" \"UA\"",
                "93.180.71.3 - - [18/May/2015:08:05:32 +0000] \"GET /b HTTP/2.0\" 404 500 \"-\" \"UA\"",
                "93.180.71.3 - - [18/May/2015:09:05:32 +0000] \"GET /b HTTP/2.0\" 404 700 \"-\" \"UA\""
            };
            File.WriteAllLines(logPath, lines, Encoding.UTF8);

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
            var r = report!;

            // Общее количество запросов
            Assert.Equal(4, report.TotalRequestsCount);

            // Размеры ответов: [100, 300, 500, 700]
            // Среднее = 400, Max = 700, P95 ≈ 670
            Assert.Equal(400, r.ResponseSizeInBytes.Average);
            Assert.Equal(700, r.ResponseSizeInBytes.Max);
            Assert.Equal(670, r.ResponseSizeInBytes.P95);

            // Ресурсы
            Assert.Equal(2, r.Resources.Count);
            Assert.Equal("/a", r.Resources[0].Resource);
            Assert.Equal(2, r.Resources[0].TotalRequestsCount);
            Assert.Equal("/b", r.Resources[1].Resource);
            Assert.Equal(2, r.Resources[1].TotalRequestsCount);

            // Коды ответа
            Assert.Equal(2, r.ResponseCodes.Count);
            Assert.Equal(200, r.ResponseCodes[0].Code);
            Assert.Equal(2, r.ResponseCodes[0].TotalResponsesCount);
            Assert.Equal(404, r.ResponseCodes[1].Code);
            Assert.Equal(2, r.ResponseCodes[1].TotalResponsesCount);

            // Распределение по датам
            Assert.NotNull(r.RequestsPerDate);
            Assert.Equal(2, r.RequestsPerDate!.Count);
            Assert.Equal(2, r.RequestsPerDate[0].TotalRequestsCount);
            Assert.Equal(50.00, r.RequestsPerDate[0].TotalRequestsPercentage, 2);
            Assert.Equal(2, r.RequestsPerDate[1].TotalRequestsCount);
            Assert.Equal(50.00, r.RequestsPerDate[1].TotalRequestsPercentage, 2);

            // Уникальные протоколы
            Assert.NotNull(r.UniqueProtocols);
            Assert.Equal(2, r.UniqueProtocols!.Count);
            Assert.Contains("HTTP/1.1", r.UniqueProtocols);
            Assert.Contains("HTTP/2.0", r.UniqueProtocols);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}