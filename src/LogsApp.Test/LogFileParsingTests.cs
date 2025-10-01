namespace Logs.Test;

public class LogFileParsingTests
{
    [Fact(DisplayName = "На вход передан валидный локальный log-файл")]
    public void LocalFileProcessingTest()
    {
        Assert.Fail("Not implemented yet");
    }

    [Fact(DisplayName = "На вход передан валидный удаленный log-файл")]
    public void RemoteFileProcessingTest()
    {
        Assert.Fail("Not implemented yet");
    }

    [Fact(DisplayName = "На вход передан валидный локальный log-файл, часть строк в котором нужно отфильтровать по --from и --to")]
    public void LocalFileProcessingAndFilteringTest()
    {
        Assert.Fail("Not implemented yet");
    }

    [Fact(DisplayName = "На вход передан локальный log-файл, часть строк в котором не подходит под формат")]
    public void DamagedLocalFileProcessingTest()
    {
        Assert.Fail("Not implemented yet");
    }
}