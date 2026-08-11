using System;

namespace VideoHarvester.App.Core
{
    internal static class UserMessageTranslator
    {
        internal static string FromDiagnosticLine(string line)
        {
            var value = (line ?? string.Empty).ToLowerInvariant();

            if (value.Contains("premium member") || value.Contains("login required"))
                return "当前画质或内容需要登录及相应账号权限。";
            if (value.Contains("requested format is not available"))
                return "所选画质当前不可用，请尝试“最佳可用画质”。";
            if (value.Contains("cookies")
                && (value.Contains("error") || value.Contains("failed") || value.Contains("could not")))
                return "浏览器登录信息读取失败，请关闭对应浏览器后重试。";
            if (value.Contains("403") || value.Contains("forbidden"))
                return "网站拒绝了当前请求，请尝试登录或稍后重试。";
            if (value.Contains("ssl") || value.Contains("tls")
                || value.Contains("timed out") || value.Contains("network"))
                return "网络连接不稳定，程序会自动重试；仍失败请检查网络。";
            if (value.Contains("private video"))
                return "这是私密视频，当前账号没有访问权限。";
            if (value.Contains("video unavailable"))
                return "该视频当前不可用或已被删除。";
            if (value.Contains("no space left"))
                return "保存磁盘空间不足。";

            return (line ?? string.Empty).StartsWith("WARNING:", StringComparison.Ordinal)
                ? "网站返回了一条提示，程序仍在继续处理。"
                : "下载失败，请复制诊断日志后发送给开发者。";
        }
    }
}
