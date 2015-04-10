namespace BaiduHiCrawlerUpdater
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Threading;

    using Newtonsoft.Json.Linq;
    using System.Diagnostics;

    class Program
    {
        static int Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            // Set current directory
            var updateDirectory = Directory.GetCurrentDirectory();

            if (args.Length > 0)
            {
                updateDirectory = args[0];

                if (Path.GetFullPath(updateDirectory) == Directory.GetCurrentDirectory())
                {
                    // Cannot update in current directory
                    Console.WriteLine();
                    Console.WriteLine("Cannot update in current directory.");
                    Thread.Sleep(1000);
                    return 1;
                }
            }
            else
            {
                // Wrong parameters
                Console.WriteLine();
                Console.WriteLine("Wrong parameters.");
                Thread.Sleep(1000);
                return 1;
            }
            
            // Wait processes to exit
            Console.WriteLine("Waiting {0} to exit...", Constants.MainProcessName);

            while (true)
            {
                Thread.Sleep(1000);

                var processes = Process.GetProcesses();

                if (processes.All(process => process.ProcessName != Constants.MainProcessName))
                {
                    break;
                }
            }

            // Get latest released version JSON object
            Console.Write("Getting latest version...");

            var latestReleasedVersionJsonObject = UpdateHelper.GetLatestReleasedVersionJsonObject();
            if (latestReleasedVersionJsonObject == null)
            {
                // Failed to get update
                Console.WriteLine();
                Console.WriteLine("Failed to get update.");
                Thread.Sleep(1000);
                return 1;
            }

            Console.WriteLine("Finished!");

            // Get update file
            if (UpdateHelper.CheckNewVersion(latestReleasedVersionJsonObject))
            {
                var downloadUrl = UpdateHelper.GetDownloadUrl(latestReleasedVersionJsonObject);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    // Failed to get update
                    Console.WriteLine();
                    Console.WriteLine("Failed to get update.");
                    Thread.Sleep(1000);
                    return 1;
                }

                Console.WriteLine("Ready to download new version {0}, current version {1} is out-of-date.",
                    latestReleasedVersionJsonObject["name"].Value<string>(), Constants.CurrentVersion);

                var localFileName = Path.GetTempFileName();
                try
                {
                    UpdateHelper.DownloadFile(downloadUrl, localFileName);
                }
                catch (Exception)
                {
                    // Failed to download update
                    Console.WriteLine();
                    Console.WriteLine("Failed to download update.");
                    Thread.Sleep(1000);
                    return 1;
                }

                // Extract to temporary directory
                Console.WriteLine("Extracting new version...");

                var tempExtractDirectory = Path.Combine(Path.GetTempPath(), Constants.TempFolderName, Guid.NewGuid().ToString());
                if (Directory.Exists(tempExtractDirectory))
                {
                    Directory.Delete(tempExtractDirectory, true);
                }
                var sourceDirectory = Directory.CreateDirectory(tempExtractDirectory);

                ZipFile.ExtractToDirectory(localFileName, tempExtractDirectory);

                // Copy to update directory
                Console.WriteLine("Updating to new version...");

                UpdateHelper.CopyFolderAndOverwrite(sourceDirectory, Directory.CreateDirectory(updateDirectory));

                // Clean up
                Directory.Delete(tempExtractDirectory, true);
            }
            else
            {
                Console.WriteLine("No new version, current version {0}", Constants.CurrentVersion);
            }

            if (args.Length > 1)
            {
                if (args[1].ToLower() == "true")
                {
                    // Restart
                    Process.Start(Path.Combine(updateDirectory, Constants.MainProcessName + ".exe"));
                }
            }

            return 0;
        }
    }
}
