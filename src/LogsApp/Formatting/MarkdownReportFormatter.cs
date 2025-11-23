using System.Globalization;
using System.Text;

namespace LogsApp;

public sealed class MarkdownReportFormatter : IReportFormatter
{
    public string Format(LogStatisticsReport stats)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#### Общая информация");
        sb.AppendLine();
        sb.AppendLine("|        Метрика        | Значение |");
        sb.AppendLine("|:---------------------:|---------:|");
        sb.AppendLine($"| Файл(-ы)              | `{string.Join("`, `", stats.Files)}` |");

        var startDate = stats.FirstRequestDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";
        var endDate = stats.LastRequestDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";

        sb.AppendLine($"| Начальная дата        | {startDate} |");
        sb.AppendLine($"| Конечная дата         | {endDate} |");
        sb.AppendLine($"| Количество запросов   | {stats.TotalRequestsCount} |");
        sb.AppendLine($"| Средний размер ответа | {stats.ResponseSizeInBytes.Average}b |");
        sb.AppendLine($"| Максимальный ответ    | {stats.ResponseSizeInBytes.Max}b |");
        sb.AppendLine($"| 95p размера ответа    | {stats.ResponseSizeInBytes.P95}b |");
        sb.AppendLine();

        // Ресурсы
        sb.AppendLine("#### Запрашиваемые ресурсы");
        sb.AppendLine();
        sb.AppendLine("| Ресурс | Количество |");
        sb.AppendLine("|:------:|-----------:|");
        foreach (var r in stats.Resources)
        {
            sb.AppendLine($"| `{r.Resource}` | {r.TotalRequestsCount} |");
        }

        sb.AppendLine();

        // Коды ответа
        sb.AppendLine("#### Коды ответа");
        sb.AppendLine();
        sb.AppendLine("| Код | Имя | Количество |");
        sb.AppendLine("|:---:|:----|-----------:|");
        foreach (var c in stats.ResponseCodes)
        {
            sb.AppendLine($"| {c.Code} | {GetReasonPhrase(c.Code)} | {c.TotalResponsesCount} |");
        }

        // Распределение по датам
        if (stats.RequestsPerDate is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("#### Распределение запросов по датам");
            sb.AppendLine();
            sb.AppendLine("| Дата | День недели | Кол-во | % от общего |");
            sb.AppendLine("|:----:|:-----------:|-------:|------------:|");

            foreach (var d in stats.RequestsPerDate)
            {
                sb.AppendLine(
                    $"| {d.Date} | {d.Weekday} | {d.TotalRequestsCount} | {d.TotalRequestsPercentage.ToString("0.00", CultureInfo.InvariantCulture)} |");
            }
        }

        // Уникальные протоколы
        if (stats.UniqueProtocols is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("#### Уникальные протоколы");
            sb.AppendLine();
            sb.AppendLine(string.Join(", ", stats.UniqueProtocols));
        }

        return sb.ToString();
    }

    internal static string GetReasonPhrase(int statusCode) =>
        statusCode switch
        {
            200 => "OK",
            201 => "Created",
            202 => "Accepted",
            204 => "No Content",
            301 => "Moved Permanently",
            302 => "Found",
            304 => "Not Modified",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "-"
        };
}
