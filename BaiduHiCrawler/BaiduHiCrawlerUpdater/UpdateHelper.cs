namespace BaiduHiCrawlerUpdater
{
    using System;
    using System.IO;
    using System.Net;

    using Newtonsoft.Json.Linq;

    public static class UpdateHelper
    {
        public static bool CheckNewVersion(string currentVersionString)
        {
            // Get current version
            var currentVersion = Version.Parse(currentVersionString);

            // Get latest version
            var latestVersionJsonObject = GetLatestReleasedVersionJsonObject();
            if (latestVersionJsonObject == null)
            {
                return false;
            }

            var latestVersionJson = latestVersionJsonObject["name"].Value<string>();
        }

        public static JObject GetLatestReleasedVersionJsonObject()
        {
            var request = WebRequest.Create(Constants.GetLatestReleaseUrlFormat);
            request.Timeout = 10000;

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
    }
}
