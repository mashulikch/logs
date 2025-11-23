using System.Globalization;

namespace LogsApp;

public enum OutputFormat
{
    Json,
    Markdown,
    Adoc
}

public sealed class AppOptions
{
    public List<string> Paths { get; } = new();
    public string Output { get; set; } = null!;
    public OutputFormat Format { get; set; } = OutputFormat.Json;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class UsageException : Exception
{
    public UsageException(string message) : base(message) { }

    public UsageException(string message, Exception inner) : base(message, inner) { }
}

public static class ArgumentParser
{
    private static readonly HashSet<string> SupportedInputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".log", ".txt" };

    public static AppOptions Parse(string[] args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        var options = new AppOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--from=", StringComparison.Ordinal))
            {
                var value = arg.Substring("--from=".Length);
                options.From = ParseIsoDate(value, "--from");
                continue;
            }

            if (arg.StartsWith("--to=", StringComparison.Ordinal))
            {
                var value = arg.Substring("--to=".Length);
                options.To = ParseIsoDate(value, "--to");
                continue;
            }

            if (arg.StartsWith("--path=", StringComparison.Ordinal))
            {
                var value = arg.Substring("--path=".Length);
                options.Paths.Add(value);
                continue;
            }

            if (arg.StartsWith("--output=", StringComparison.Ordinal))
            {
                var value = arg.Substring("--output=".Length);
                options.Output = value;
                continue;
            }

            if (arg.StartsWith("--format=", StringComparison.Ordinal))
            {
                var value = arg.Substring("--format=".Length);
                var inlineFormat = value.ToLowerInvariant();
                options.Format = inlineFormat switch
                {
                    "json" => OutputFormat.Json,
                    "markdown" => OutputFormat.Markdown,
                    "md" => OutputFormat.Markdown,
                    "adoc" => OutputFormat.Adoc,
                    _ => throw new UsageException($"Unsupported output format '{inlineFormat}'")
                };

                continue;
            }

            string NextValue()
            {
                if (i + 1 >= args.Length)
                {
                    throw new UsageException($"Argument '{arg}' requires a value");
                }

                return args[++i];
            }

            switch (arg)
            {
                case "--path":
                case "-p":
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new UsageException($"Argument '{arg}' requires at least one value");
                    }

                    var j = i + 1;

                    while (j < args.Length && !args[j].StartsWith("-", StringComparison.Ordinal))
                    {
                        options.Paths.Add(args[j]);
                        j++;
                    }

                    i = j - 1;
                    break;
                }

                case "--output":
                case "-o":
                    options.Output = NextValue();
                    break;

                case "--format":
                case "-f":
                {
                    var formatStr = NextValue().ToLowerInvariant();
                    options.Format = formatStr switch
                    {
                        "json" => OutputFormat.Json,
                        "markdown" => OutputFormat.Markdown,
                        "md" => OutputFormat.Markdown,
                        "adoc" => OutputFormat.Adoc,
                        _ => throw new UsageException($"Unsupported output format '{formatStr}'")
                    };
                    break;
                }

                case "--from":
                    options.From = ParseIsoDate(NextValue(), "--from");
                    break;

                case "--to":
                    options.To = ParseIsoDate(NextValue(), "--to");
                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new UsageException($"Unsupported argument '{arg}'");
                    }

                    break;
            }
        }

        if (options.Paths.Count == 0)
        {
            throw new UsageException("Required argument '--path' is missing");
        }

        if (string.IsNullOrWhiteSpace(options.Output))
        {
            throw new UsageException("Required argument '--output' is missing");
        }

        if (options.From is not null && options.To is not null && options.From >= options.To)
        {
            throw new UsageException("Parameter '--from' must be less than '--to'");
        }

        ValidateOutputPath(options.Output, options.Format);
        ValidateInputPaths(options.Paths);

        return options;
    }

    private static DateTime ParseIsoDate(string value, string argumentName)
    {
        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out var dt))
        {
            throw new UsageException($"Value '{value}' of argument '{argumentName}' is not a valid ISO8601 date");
        }

        return dt;
    }

    private static void ValidateOutputPath(string outputPath, OutputFormat format)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(directory))
        {
            throw new UsageException($"Output directory '{directory}' does not exist");
        }

        var ext = Path.GetExtension(outputPath).ToLowerInvariant();

        var expectedExt = format switch
        {
            OutputFormat.Json => ".json",
            OutputFormat.Markdown => ".md",
            OutputFormat.Adoc => ".ad",
            _ => throw new UsageException("Unsupported output format")
        };

        if (!string.Equals(ext, expectedExt, StringComparison.OrdinalIgnoreCase))
        {
            throw new UsageException(
                $"Output file extension '{ext}' does not match expected '{expectedExt}' for selected format");
        }

        if (File.Exists(outputPath))
        {
            throw new UsageException($"Output file '{outputPath}' already exists");
        }
    }

    private static void ValidateInputPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var remoteExt = Path.GetExtension(uri.LocalPath);
                if (!string.IsNullOrEmpty(remoteExt) &&
                    !SupportedInputExtensions.Contains(remoteExt))
                {
                    throw new UsageException(
                        $"Remote file '{path}' has unsupported extension '{remoteExt}'");
                }

                continue;
            }

            if (HasWildcards(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = Directory.GetCurrentDirectory();
                }

                if (!Directory.Exists(dir))
                {
                    throw new UsageException(
                        $"Directory '{dir}' not found for pattern '{path}'");
                }

                var pattern = Path.GetFileName(path);
                var files = Directory.EnumerateFiles(dir, pattern).ToList();

                if (files.Count == 0)
                {
                    throw new UsageException(
                        $"No files matched path pattern '{path}'");
                }

                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file);
                    if (!SupportedInputExtensions.Contains(ext))
                    {
                        throw new UsageException(
                            $"File '{file}' has unsupported extension '{ext}'");
                    }
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    throw new UsageException($"File '{path}' not found");
                }

                var ext = Path.GetExtension(path);
                if (!SupportedInputExtensions.Contains(ext))
                {
                    throw new UsageException(
                        $"File '{path}' has unsupported extension '{ext}'");
                }
            }
        }
    }

    private static bool HasWildcards(string path) =>
        path.IndexOfAny(new[] { '*', '?' }) >= 0;
}
