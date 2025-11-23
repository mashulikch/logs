using System.Globalization;
using System.Text;

namespace LogsApp;

public sealed class AdocReportFormatter : IReportFormatter
{
    public string Format(LogStatisticsReport stats)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Общая информация");
        sb.AppendLine();
        sb.AppendLine("[options=\"header\"]");
        sb.AppendLine("|===");
        sb.AppendLine("| Метрика | Значение");
        sb.AppendLine($"| Файл(-ы) | {string.Join(", ", stats.Files)}");

        var startDate = stats.FirstRequestDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";
        var endDate = stats.LastRequestDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";

        sb.AppendLine($"| Начальная дата | {startDate}");
        sb.AppendLine($"| Конечная дата | {endDate}");
        sb.AppendLine($"| Количество запросов | {stats.TotalRequestsCount}");
        sb.AppendLine($"| Средний размер ответа | {stats.ResponseSizeInBytes.Average}b");
        sb.AppendLine($"| Максимальный размер ответа | {stats.ResponseSizeInBytes.Max}b");
        sb.AppendLine($"| 95p размера ответа | {stats.ResponseSizeInBytes.P95}b");
        sb.AppendLine("|===");
        sb.AppendLine();

        sb.AppendLine("=== Запрашиваемые ресурсы");
        sb.AppendLine("[options=\"header\"]");
        sb.AppendLine("|===");
        sb.AppendLine("| Ресурс | Количество");
        foreach (var r in stats.Resources)
        {
            sb.AppendLine($"| {r.Resource} | {r.TotalRequestsCount}");
        }

        sb.AppendLine("|===");
        sb.AppendLine();

        sb.AppendLine("=== Коды ответа");
        sb.AppendLine("[options=\"header\"]");
        sb.AppendLine("|===");
        sb.AppendLine("| Код | Имя | Количество");
        foreach (var c in stats.ResponseCodes)
        {
            sb.AppendLine($"| {c.Code} | {MarkdownReportFormatter.GetReasonPhrase(c.Code)} | {c.TotalResponsesCount}");
        }

        sb.AppendLine("|===");
        sb.AppendLine();

        if (stats.RequestsPerDate is { Count: > 0 })
        {
            sb.AppendLine("=== Распределение запросов по датам");
            sb.AppendLine("[options=\"header\"]");
            sb.AppendLine("|===");
            sb.AppendLine("| Дата | День недели | Кол-во | % от общего");
            foreach (var d in stats.RequestsPerDate)
            {
                sb.AppendLine(
                    $"| {d.Date} | {d.Weekday} | {d.TotalRequestsCount} | {d.TotalRequestsPercentage.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            sb.AppendLine("|===");
            sb.AppendLine();
        }

        if (stats.UniqueProtocols is { Count: > 0 })
        {
            sb.AppendLine("=== Уникальные протоколы");
            sb.AppendLine();
            sb.AppendLine(string.Join(", ", stats.UniqueProtocols));
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
