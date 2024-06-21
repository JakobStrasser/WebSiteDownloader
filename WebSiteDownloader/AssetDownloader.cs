using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace RecursiveWebDownloader
{
    public class AssetDownloader
    {
       
        private Uri _root;

        public AssetDownloader(Uri root)
        {
            _root = root;
        }



        public List<Uri> GetMatchingLinks(HtmlDocument doc, Uri pageUrl, string folderPath, string xpath, string attribute)
        {
            List<Uri> assets = new List<Uri>();
            foreach (var node in doc.DocumentNode.SelectNodes(xpath))
            {
                var url = node.GetAttributeValue(attribute, string.Empty);
                var assetUrl = new Uri(pageUrl, url);
                //External link so skip
                if (_root.Host != assetUrl.Host)
                    continue;
                assets.Add(assetUrl);
            }
            return assets;
        }

        public async Task<string> DownloadAssetAsync(Uri uri, string folderPath)
        {
            try
            {
                if (!HttpClientPool.Initialized)
                    HttpClientPool.Initialize(10);
                var _httpClient = await HttpClientPool.GetClientAsync();
                var assetContent = await _httpClient.GetByteArrayAsync(uri);
                HttpClientPool.ReleaseClient(_httpClient);

                var assetRelativePath = FileHelper.GetRelativePath(_root, uri);
                var assetFullPath = Path.Combine(folderPath, assetRelativePath);

                FileHelper.EnsureDirectoryCreated(assetFullPath);

                lock (this)
                {
                    int maxTries = 3;

                    for (int i = 0; i < maxTries; i++)
                    {
                        try
                        {
                            using (var f = new FileStream(assetFullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                            {
                                f.Write(assetContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving {uri}: {ex.Message}");
                            Thread.Sleep(1000);
                            if (i == maxTries - 1)
                                throw;
                        }
                    }
                    
                }
                return assetRelativePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading asset {uri}: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
                return uri.ToString(); 
            }
        }
    }
}
