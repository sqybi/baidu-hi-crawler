namespace BaiduHiCrawlerUpdater
{
    public static class Constants
    {
        public const string GitHubApiBaseUrl = "https://api.github.com";

        public const string Owner = "sqybi";

        public const string Repo = "baidu-hi-crawler";

        public static readonly string GetLatestReleaseUrlFormat = string.Format(
            "{0}/repos/{1}/{2}/releases/latest",
            GitHubApiBaseUrl,
            Owner,
            Repo);
    }
}