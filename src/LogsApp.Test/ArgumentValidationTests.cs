namespace LogsApp.Test
{
    public class ArgumentValidationTests
    {
        [Fact(DisplayName =  "На вход передан несуществующий локальный файл")]
        public void Test1_OnInputWithNonExistentLocalFile()
        {
            Assert.Fail("Not implemented yet");
        }

        [Fact(DisplayName =  "На вход передан несуществующий удаленный файл")]
        public void Test2_OnInputWithNonExistentRemoteFile()
        {
            Assert.Fail("Not implemented yet");
        }

        [Theory(DisplayName =  "На вход передан файл в неподдерживаемом формате")]
        [InlineData(".docx")]
        public void Test3_OnInputWithUnsupportedFileFormat(string extension)
        {
            Assert.Fail("Not implemented yet");
        }

        [Theory(DisplayName = "На вход переданы невалидные параметры --from / --to")]
        [MemberData(nameof(Test4ArgumentsSource))]
        public void Test4_OnInputWithInvalidFromOrToParameters(string from, string to)
        {
            Assert.Fail("Not implemented yet");
        }

        [Theory(DisplayName = "Результаты запрошены в неподдерживаемом формате")]
        [InlineData("txt")]
        public void Test5_OnInputWithUnsupportedOutputFormat(string format)
        {
            Assert.Fail("Not implemented yet");
        }

        [Theory(DisplayName = "По пути в аргументе --output указан файл с некоректным расширением")]
        [MemberData(nameof(Test6ArgumentsSource))]
        public void Test6_OnOutputArgumentHasIncorrectExtension(string format, string output)
        {
            Assert.Fail("Not implemented yet");
        }

        [Fact(DisplayName =  "По пути в аргументе --output уже существует файл")]
        public void Test7_OnOutputArgumentPointsToFileThatAlreadyExists()
        {
            Assert.Fail("Not implemented yet");
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
            Assert.Fail("Not implemented yet");
        }

        [Theory(DisplayName = "На вход передан неподдерживаемый параметр")]
        [InlineData("--input")]
        [InlineData("--filter")]
        public void Test9_OnUnsupportedParameterProvided(string argument)
        {
            Assert.Fail("Not implemented yet");
        }

        [Fact(DisplayName = "На вход передан параметр --from, значение которого больше, чем --to")]
        public void Test10_WhenFromParameterIsGreaterThanToParameter()
        {
            Assert.Fail("Not implemented yet");
        }

        public static TheoryData<string, string> Test4ArgumentsSource => new() {{"2025.01.01 10:30", "today"}};

        public static TheoryData<string, string> Test6ArgumentsSource => new()
        {
            { "markdown", "./results.txt" },
            { "json", "./results.md" },
            { "adoc", "./results.ad1" }
        };
    }
}
