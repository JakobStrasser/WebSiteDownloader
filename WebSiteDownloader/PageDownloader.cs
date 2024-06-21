using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RecursiveWebDownloader
{
    public class PageDownloader
    {
        private static Uri _root;
        private readonly BlockingCollection<string> visitedUrls = new BlockingCollection<string>();
        private readonly AssetDownloader assetDownloader;
        private readonly HtmlParser htmlParser = new HtmlParser();
        private int counter = 0;

        public PageDownloader(Uri root)
        {
            _root = root;
            assetDownloader = new AssetDownloader(_root);

        }

        public async Task DownloadPageAsync(Uri url, string folderPath)
        {
            if (visitedUrls.Contains(url.ToString())) return;
            visitedUrls.Add(url.ToString());

            var extension = Path.GetExtension(url.ToString());

            switch (extension)
            {
                case ".html":
                    await DownloadHtmlAsync(url, folderPath);
                    break;
                case ".htm":
                    await DownloadHtmlAsync(url, folderPath);
                    break;
                default:
                    await assetDownloader.DownloadAssetAsync(url, folderPath);
                    break;
            }
        }

        public async Task DownloadHtmlAsync(Uri url, string folderPath)
        {
            try
            {
                if (!HttpClientPool.Initialized)
                    HttpClientPool.Initialize(10);
                var _httpClient = await HttpClientPool.GetClientAsync();
                string content = await _httpClient.GetStringAsync(url);
                HttpClientPool.ReleaseClient(_httpClient);

                string relativePath = FileHelper.GetRelativePath(_root, url);
                string filename = Path.GetFileName(url.ToString());
                string filepath = Path.Combine(folderPath, relativePath);
                FileHelper.EnsureDirectoryCreated(filepath);



                var doc = htmlParser.ParseHtml(content);

                List<Uri> links = GetLinksFromDocument(url, folderPath, doc);

                await CreateAndRunDownloadTasks(folderPath, links);

                //Failsafe for filename
                if (Directory.Exists(filepath))
                    filepath = Path.Combine(filepath, filename);

                lock (this)
                {
                    using var f = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                    f.Write(new UTF8Encoding().GetBytes(doc.DocumentNode.OuterHtml));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading {url}: {ex.Message}");
            }
            Interlocked.Increment(ref counter);
            if (counter % 100 == 0)
                Console.WriteLine($"Donwloaded {counter} pages");
        }

        private async Task CreateAndRunDownloadTasks(string folderPath, List<Uri> links)
        {
            var tasks = new List<Task>();
            foreach (var link in links)
            {
                //Already visited skip
                if (visitedUrls.Contains(link.ToString())) continue;
                //External url skip
                if (_root.Host != link.Host)
                    continue;
                tasks.Add(Task.Run(async () => await DownloadPageAsync(link, folderPath)));
            }

            await Task.WhenAll(tasks);
        }

        private List<Uri> GetLinksFromDocument(Uri url, string folderPath, HtmlDocument doc)
        {
            var links = htmlParser.GetLinks(doc, url);

            // Images
            links.AddRange(assetDownloader.GetMatchingLinks(doc, url, folderPath, "//img[@src]", "src"));
            // JavaScript
            links.AddRange(assetDownloader.GetMatchingLinks(doc, url, folderPath, "//script[@src]", "src"));
            // CSS
            links.AddRange(assetDownloader.GetMatchingLinks(doc, url, folderPath, "//link[@rel='stylesheet']", "href"));
            return links;
        }
    }

}

