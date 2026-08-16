using System;
using VideoHarvester.App.Core;

namespace VideoHarvester.Core.Tests
{
    internal static class Program
    {
        private static int failures;

        private static void Main()
        {
            Check("YouTube playlist query", DownloadRules.IsYouTubePlaylist(
                "https://www.youtube.com/watch?v=abc123&list=PL123"));
            Check("YouTube playlist page", DownloadRules.IsYouTubePlaylist(
                "https://www.youtube.com/playlist?list=PL123"));
            Check("Single YouTube video", !DownloadRules.IsYouTubePlaylist(
                "https://www.youtube.com/watch?v=abc123"));

            Equal("YouTube key", "Youtube:abc123XYZ",
                DownloadRules.CreateDownloadKey("https://youtu.be/abc123XYZ"));
            Equal("Bilibili key", "BiliBili:BV1Qh41187jq_p3",
                DownloadRules.CreateDownloadKey("https://www.bilibili.com/video/BV1Qh41187jq?p=3"));
            Equal("Unknown URL key", "URL:https://example.com/video/1",
                DownloadRules.CreateDownloadKey("https://example.com/video/1"));

            Equal("Byte formatting", "1.0 MB", DownloadRules.FormatBytes(1024 * 1024));
            Equal("Unknown byte formatting", "--", DownloadRules.FormatBytes(0));
            Equal("Filename limit", 100, DownloadRules.CreateSafeFileName(new string('a', 105)).Length);
            Equal("Completed filename media id", "_GD1VIcs9Kg",
                DownloadRules.ExtractBracketedMediaId("Example [_GD1VIcs9Kg]-1786726779.mp4"));
            Equal("Partial filename media id", "_GD1VIcs9Kg",
                DownloadRules.ExtractBracketedMediaId("Example [_GD1VIcs9Kg]-1786726766.f303.webm.part"));
            Equal("Reusable automatic suffix", "1786726766",
                DownloadRules.ExtractAutomaticSuffixFromPartialFile(
                    "Example [_GD1VIcs9Kg]-1786726766.f303.webm.part", "Youtube:_GD1VIcs9Kg"));
            Equal("Reject suffix from another video", string.Empty,
                DownloadRules.ExtractAutomaticSuffixFromPartialFile(
                    "Example [otherVideo1]-1786726766.f303.webm.part", "Youtube:_GD1VIcs9Kg"));
            Equal("Bilibili key media id", "BV1Qh41187jq",
                DownloadRules.ExtractMediaIdFromKey("BiliBili:BV1Qh41187jq_p3"));
            int removedUrls;
            var remainingUrls = DownloadRules.RemoveMatchingUrlLines(
                new[] { " https://example.com/done ", "https://example.com/failed", "" },
                new[] { "https://example.com/done" }, out removedUrls);
            Equal("Completed links removed", 1, removedUrls);
            Equal("Unfinished link retained", "https://example.com/failed", remainingUrls[0]);

            Equal("Format error translation", "所选画质当前不可用，请尝试“最佳可用画质”。",
                UserMessageTranslator.FromDiagnosticLine("ERROR: Requested format is not available"));
            Equal("Network error translation", "网络连接不稳定，程序会自动重试；仍失败请检查网络。",
                UserMessageTranslator.FromDiagnosticLine("ERROR: TLS connection timed out"));
            Equal("Warning fallback", "网站返回了一条提示，程序仍在继续处理。",
                UserMessageTranslator.FromDiagnosticLine("WARNING: extractor message"));
            Equal("Browser cookie lock translation", "浏览器登录信息读取失败，软件将自动改用未登录模式继续。",
                UserMessageTranslator.FromDiagnosticLine("ERROR: Could not copy Chrome cookie database"));

            if (failures > 0)
            {
                Console.Error.WriteLine("Core checks failed: " + failures);
                Environment.Exit(1);
            }

            Console.WriteLine("All core checks passed.");
        }

        private static void Check(string name, bool condition)
        {
            if (condition)
            {
                Console.WriteLine("PASS  " + name);
                return;
            }

            failures++;
            Console.Error.WriteLine("FAIL  " + name);
        }

        private static void Equal<T>(string name, T expected, T actual)
        {
            Check(name, Equals(expected, actual));
            if (!Equals(expected, actual))
            {
                Console.Error.WriteLine("      Expected: " + expected);
                Console.Error.WriteLine("      Actual:   " + actual);
            }
        }
    }
}
