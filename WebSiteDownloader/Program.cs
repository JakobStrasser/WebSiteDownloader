using System;
using System.Threading.Tasks;

namespace RecursiveWebDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Uri uri = new Uri(@"https://books.toscrape.com/index.html");
            Console.WriteLine($"Downloading uri: {uri}");
           
            string folderPath = @"c:\temp\bookstoscrapecom";
            Console.WriteLine($"Saving to local folder: {folderPath}");
            FileHelper.EnsureDirectoryCreated(folderPath);
            FileHelper.EnsureTargetDirectoryIsEmpty(folderPath);

            var pageDownloader = new PageDownloader(uri);
            await pageDownloader.DownloadPageAsync(uri, folderPath);

            Console.WriteLine("Download completed.");
            Console.ReadLine();
        }
    }
}
