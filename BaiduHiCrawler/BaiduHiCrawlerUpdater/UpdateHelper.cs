namespace BaiduHiCrawlerUpdater
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;

    using Newtonsoft.Json.Linq;

    public static class UpdateHelper
    {
        private static readonly object ProgressUpdateLock = new object();

        public static bool CheckNewVersion(JObject latestVersionJsonObject)
        {
            try
            {
                // Get latest version
                if (latestVersionJsonObject == null)
                {
                    return false;
                }

                var latestVersionString = latestVersionJsonObject["tag_name"].Value<string>();
                var latestVersion = Version.Parse(latestVersionString);

                // Check update
                return Constants.CurrentVersion.CompareTo(latestVersion) < 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetDownloadUrl(JObject latestVersionJsonObject)
        {
            if (latestVersionJsonObject == null)
            {
                return null;
            }

            try
            {
                foreach (var assetJsonObject in latestVersionJsonObject["assets"])
                {
                    if (string.Compare(assetJsonObject["name"].Value<string>(), Constants.ZipFileName,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0)
                    {
                        return assetJsonObject["browser_download_url"].Value<string>();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public static JObject GetLatestReleasedVersionJsonObject()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            var request = WebRequest.CreateHttp(Constants.GetLatestReleaseUrlFormat);
            request.Timeout = 10000;
            request.UserAgent = Constants.GitHubAppName;

            try
            {
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        return null;
                    }

                    using (var responseStreamReader = new StreamReader(responseStream))
                    {
                        var responseText = responseStreamReader.ReadToEnd();

                        var responseJsonObject = JObject.Parse(responseText);

                        return responseJsonObject;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void DownloadFile(string url, string localFileName)
        {
            Console.WriteLine("Downloading from {0} to {1}...", url, localFileName);

            var request = WebRequest.CreateHttp(url);
            request.AllowAutoRedirect = false;
            using (var response = request.GetResponse())
            {
                if (response.Headers["Location"] != null)
                {
                    url = response.Headers["Location"];
                }
            }

            var webClient = new WebClient();

            long previousProgressPercentage = 0;
            webClient.DownloadProgressChanged += (sender, args) =>
            {
                var currentProgressPercentage = args.BytesReceived * 100 / args.TotalBytesToReceive;
                lock (ProgressUpdateLock)
                {
                    if (currentProgressPercentage > previousProgressPercentage)
                    {
                        UpdateProgress(currentProgressPercentage);
                        previousProgressPercentage = currentProgressPercentage;
                    }
                }
            };

            webClient.DownloadFileTaskAsync(url, localFileName).Wait();

            UpdateProgress(100);

            Console.WriteLine("Finished!");
        }

        public static void UpdateProgress(long progressPercentage)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("Progress: {0}%... ", progressPercentage);
        }

        public static void CopyFolderAndOverwrite(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory)
        {
            foreach (var directory in sourceDirectory.GetDirectories())
            {
                CopyFolderAndOverwrite(directory, targetDirectory.CreateSubdirectory(directory.Name));
            }

            foreach (var file in sourceDirectory.GetFiles())
            {
                file.CopyTo(Path.Combine(targetDirectory.FullName, file.Name), true);
            }
        }
    }
}
