namespace BaiduHiCrawlerUpdater
{
    public static class Constants
    {
        /// <summary>
        /// Gets current version
        /// </summary>
        /// <remarks>Please update this variable each time distributing a new version!!!</remarks>
        public static readonly Version CurrentVersion = new Version
        {
            Major = 0,
            Minor = 2,
            Revision = 2
        };

        public const string MainProcessName = "BaiduHiCrawler";

        public const string MainFileName = MainProcessName + ".exe";

        public const string UpdaterFileName = @".\BaiduHiCrawlerUpdater.exe";

        public static readonly string[] UpdaterDependentFileNames = { @".\Newtonsoft.Json.dll" };

        public const string TempFolderName = @"BaiduHiCrawler";

        public const string GitHubToken = "3bfcbfa467efe5d3119a2f322ed9ac03d64eb8dd";

        public const string GitHubAppName = "baidu-hi-crawler";

        public const string GitHubApiBaseUrl = @"https://api.github.com";

        public const string Owner = "sqybi";

        public const string Repo = "baidu-hi-crawler";

        public const string ZipFileName = "BaiduHiCrawler.zip";

        public static readonly string GetLatestReleaseUrlFormat = string.Format(
            "{0}/repos/{1}/{2}/releases/latest?access_token={3}",
            GitHubApiBaseUrl,
            Owner,
            Repo,
            GitHubToken);
    }
}