using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using LogsApp;
using System.Net;

namespace LogsApp.Test;

[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
public class ArgumentValidationTests
{
    [Fact(DisplayName = "На вход передан несуществующий локальный файл")]
    public void Test1_OnInputWithNonExistentLocalFile()
    {
        var args = new[]
        {
            "--path", "this-file-definitely-does-not-exist.log",
            "--output", "result.json",
            "--format", "json"
        };

        var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "На вход передан несуществующий удаленный файл")]
    public void Test2_OnInputWithNonExistentRemoteFile()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, string.Empty);
        var httpClient = new HttpClient(handler);

        var options = new AppOptions();
        options.Paths.Add("http://unit.test/missing.log");

        var processor = new LogProcessor(httpClient);

        var ex = Assert.Throws<UsageException>(() => processor.Process(options));
        Assert.Contains("404", ex.Message);
    }


    [Theory(DisplayName = "На вход передан файл в неподдерживаемом формате")]
    [InlineData(".docx")]
    public void Test3_OnInputWithUnsupportedFileFormat(string extension)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"logsapp_test{extension}");
        File.WriteAllText(tmpFile, "dummy");

        try
        {
            var args = new[]
            {
                "--path", tmpFile,
                "--output", "result.json",
                "--format", "json"
            };

            var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
            Assert.Contains("unsupported", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    [Theory(DisplayName = "На вход переданы невалидные параметры --from / --to")]
    [MemberData(nameof(Test4ArgumentsSource))]
    public void Test4_OnInputWithInvalidFromOrToParameters(string from, string to)
    {
        var args = new List<string>
        {
            "--path", "dummy.log",
            "--output", "result.json",
            "--format", "json",
            "--from", from,
            "--to", to
        }.ToArray();

        var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
        Assert.Contains("ISO8601", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory(DisplayName = "Результаты запрошены в неподдерживаемом формате")]
    [InlineData("txt")]
    public void Test5_OnInputWithUnsupportedOutputFormat(string format)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), "logsapp_valid.log");
        File.WriteAllText(tmpFile, "dummy");

        try
        {
            var args = new[]
            {
                "--path", tmpFile,
                "--output", "result.json",
                "--format", format
            };

            var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
            Assert.Contains("unsupported output format", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    [Theory(DisplayName = "По пути в аргументе --output указан файл с некоректным расширением")]
    [MemberData(nameof(Test6ArgumentsSource))]
    public void Test6_OnOutputArgumentHasIncorrectExtension(string format, string output)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), "logsapp_valid.log");
        File.WriteAllText(tmpFile, "dummy");

        try
        {
            var args = new[]
            {
                "--path", tmpFile,
                "--output", output,
                "--format", format
            };

            var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
            Assert.Contains("extension", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    [Fact(DisplayName = "По пути в аргументе --output уже существует файл")]
    public void Test7_OnOutputArgumentPointsToFileThatAlreadyExists()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), "logsapp_valid.log");
        File.WriteAllText(tmpFile, "dummy");

        var existingOutput = Path.Combine(Path.GetTempPath(), "logsapp_existing.json");
        File.WriteAllText(existingOutput, "{}");

        try
        {
            var args = new[]
            {
                "--path", tmpFile,
                "--output", existingOutput,
                "--format", "json"
            };

            var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
            Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }

            if (File.Exists(existingOutput))
            {
                File.Delete(existingOutput);
            }
        }
    }

    [Theory(DisplayName = "На вход не передан обязательный параметр")]
    [InlineData("--path")]
    [InlineData("--output")]
    [InlineData("--format")]
    [InlineData("-p")]
    [InlineData("-o")]
    [InlineData("-f")]
    public void Test8_OnMissingRequiredParameter(string argument)
    {
        var args = new[] { argument };

        var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
        Assert.Contains("requires a value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory(DisplayName = "На вход передан неподдерживаемый параметр")]
    [InlineData("--input")]
    [InlineData("--filter")]
    public void Test9_OnUnsupportedParameterProvided(string argument)
    {
        var args = new[] { argument };

        var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
        Assert.Contains("Unsupported argument", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "На вход передан параметр --from, значение которого больше, чем --to")]
    public void Test10_WhenFromParameterIsGreaterThanToParameter()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), "logsapp_valid.log");
        File.WriteAllText(tmpFile, "dummy");

        try
        {
            var args = new[]
            {
                "--path", tmpFile,
                "--output", "result.json",
                "--format", "json",
                "--from", "2025-01-02",
                "--to", "2025-01-01"
            };

            var ex = Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
            Assert.Contains("--from", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    public static TheoryData<string, string> Test4ArgumentsSource => new()
    {
        { "2025.01.01 10:30", "today" }
    };

    public static TheoryData<string, string> Test6ArgumentsSource => new()
    {
        { "markdown", "./results.txt" },
        { "json", "./results.md" },
        { "adoc", "./results.ad1" }
    };
}