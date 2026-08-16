using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VideoHarvester.App.Core
{
    internal static class DownloadRules
    {
        private static readonly Regex YouTubeListPattern = new Regex(
            @"(?:youtube\.com|youtu\.be).*[?&]list=[^&]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YouTubePlaylistPagePattern = new Regex(
            @"youtube\.com/playlist\?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YouTubeIdPattern = new Regex(
            @"(?:v=|youtu\.be/)([0-9A-Za-z_-]{6,})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BilibiliIdPattern = new Regex(
            @"(BV[0-9A-Za-z]+)(?:.*?[?&]p=(\d+))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BracketedMediaIdPattern = new Regex(
            @"\[([0-9A-Za-z_-]{6,})\](?:-([0-9]+))?(?:\.[^.]+)+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static bool IsYouTubePlaylist(string url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && (YouTubeListPattern.IsMatch(url) || YouTubePlaylistPagePattern.IsMatch(url));
        }

        internal static string CreateDownloadKey(string url)
        {
            var youtube = YouTubeIdPattern.Match(url ?? string.Empty);
            if (youtube.Success)
            {
                return "Youtube:" + youtube.Groups[1].Value;
            }

            var bilibili = BilibiliIdPattern.Match(url ?? string.Empty);
            if (bilibili.Success)
            {
                return "BiliBili:" + bilibili.Groups[1].Value
                    + (bilibili.Groups[2].Success ? "_p" + bilibili.Groups[2].Value : string.Empty);
            }

            return "URL:" + url;
        }

        internal static string CreateSafeFileName(string text)
        {
            var result = text ?? string.Empty;
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(character, '_');
            }

            return result.Length > 100 ? result.Substring(0, 100) : result;
        }

        internal static string ExtractBracketedMediaId(string path)
        {
            var match = BracketedMediaIdPattern.Match(Path.GetFileName(path ?? string.Empty));
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        internal static string ExtractMediaIdFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (key.StartsWith("Youtube:", StringComparison.OrdinalIgnoreCase))
            {
                return key.Substring("Youtube:".Length);
            }

            if (key.StartsWith("BiliBili:", StringComparison.OrdinalIgnoreCase))
            {
                var value = key.Substring("BiliBili:".Length);
                var match = Regex.Match(value, @"^(BV[0-9A-Za-z]+)(?:_p\d+)?$", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value : string.Empty;
            }

            return string.Empty;
        }

        internal static string ExtractAutomaticSuffixFromPartialFile(string path, string key)
        {
            if (string.IsNullOrWhiteSpace(path)
                || !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var expectedId = ExtractMediaIdFromKey(key);
            var match = BracketedMediaIdPattern.Match(Path.GetFileName(path));
            if (!match.Success || !match.Groups[2].Success
                || !string.Equals(expectedId, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return match.Groups[2].Value;
        }

        internal static string[] RemoveMatchingUrlLines(string[] lines, IEnumerable<string> urls, out int removed)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            if (urls != null)
            {
                foreach (var url in urls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        matches.Add(url.Trim());
                    }
                }
            }

            var remaining = new List<string>();
            removed = 0;
            if (lines == null)
            {
                return remaining.ToArray();
            }

            foreach (var line in lines)
            {
                if (matches.Contains((line ?? string.Empty).Trim()))
                {
                    removed++;
                    continue;
                }

                remaining.Add(line);
            }

            return remaining.ToArray();
        }

        internal static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
            {
                return "--";
            }

            double value = bytes;
            var units = new[] { "B", "KB", "MB", "GB", "TB" };
            var unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return value.ToString(unitIndex < 2 ? "0" : "0.0") + " " + units[unitIndex];
        }
    }
}
