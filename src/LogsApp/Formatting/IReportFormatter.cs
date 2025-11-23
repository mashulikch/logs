namespace LogsApp;

public interface IReportFormatter
{
    string Format(LogStatisticsReport stats);
}