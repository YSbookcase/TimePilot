using System.Text.Json;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppSettingsWebSearchTests
    {
        [Theory]
        [InlineData("{}")]
        [InlineData("{\"KoreanWebSearchSuffix\":null}")]
        [InlineData("invalid json")]
        public void Load_OldOrInvalidSettingsKeepOriginalSuffix(string json)
        {
            WithSettingsFile(path =>
            {
                File.WriteAllText(path, json);
                Assert.Equal("이란", AppSettings.Load(path).KoreanWebSearchSuffix);
            });
        }

        [Theory]
        [InlineData(" 사용법 ", "사용법")]
        [InlineData("이란?", "이란?")]
        [InlineData("", "")]
        [InlineData(" \t ", "")]
        [InlineData(null, "이란")]
        public void SetKoreanWebSearchSuffix_PersistsCustomAndEmptyValues(string? input, string expected)
        {
            WithSettingsFile(path =>
            {
                var settings = AppSettings.Load(path);
                Assert.Equal("이란", settings.KoreanWebSearchSuffix);
                settings.SetKoreanWebSearchSuffix(input);
                settings.SetUiLanguage(UiLanguage.English);

                var reloaded = AppSettings.Load(path);
                Assert.Equal(expected, reloaded.KoreanWebSearchSuffix);
                Assert.Equal(UiLanguage.English, reloaded.UiLanguage);
                using var persisted = JsonDocument.Parse(File.ReadAllText(path));
                Assert.Equal(expected, persisted.RootElement.GetProperty("KoreanWebSearchSuffix").GetString());
            });
        }

        private static void WithSettingsFile(Action<string> test)
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotWebSearch-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            try
            {
                test(Path.Combine(root, "settings.json"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
