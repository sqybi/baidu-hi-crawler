namespace BaiduHiCrawlerUpdater
{
    using System.IO;
    using System.IO.Compression;

    class Program
    {
        static void Main(string[] args)
        {
            // Set current directory
            var updateDirectory = Directory.GetCurrentDirectory();

            if (args.Length > 0)
            {
                updateDirectory = args[0];
            }

            Directory.SetCurrentDirectory(updateDirectory);

            // Get update file


            ZipFile.ExtractToDirectory();
        }
    }
}
