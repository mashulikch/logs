using Serilog;

namespace LogsApp;

public static class Program
{
    public static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            return Run(args);
        }
        catch (UsageException ex)
        {
            Log.Error(ex, "Incorrect program usage: {Message}", ex.Message);
            return 2;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unexpected error");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static int Run(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Log.Information("Starting log analyzer");
        Log.Information("Paths: {Paths}", string.Join(", ", options.Paths));
        Log.Information("Output: {Output}", options.Output);
        Log.Information("Format: {Format}", options.Format.ToString().ToLowerInvariant());
        if (options.From is not null || options.To is not null)
        {
            Log.Information("Date filter: from {From} to {To}", options.From, options.To);
        }

        var processor = new LogProcessor();
        var stats = processor.Process(options);

        IReportFormatter formatter = options.Format switch
        {
            OutputFormat.Json => new JsonReportFormatter(),
            OutputFormat.Markdown => new MarkdownReportFormatter(),
            OutputFormat.Adoc => new AdocReportFormatter(),
            _ => throw new UsageException("Unsupported output format")
        };

        var reportText = formatter.Format(stats);

        WriteReportToFile(options.Output, reportText);

        Log.Information("Finished successfully");
        return 0;
    }

    private static void WriteReportToFile(string outputPath, string content)
    {
        try
        {
            using var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(content);
        }
        catch (IOException ex)
        {
            throw new UsageException($"Failed to write output file '{outputPath}': {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UsageException($"Access denied for output file '{outputPath}'", ex);
        }
    }
}
