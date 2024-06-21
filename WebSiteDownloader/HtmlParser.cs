using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace RecursiveWebDownloader
{
    public class HtmlParser
    {
        public HtmlDocument ParseHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        public List<Uri> GetLinks(HtmlDocument doc, Uri baseUrl)
        {
            var links = new List<Uri>();
            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var href = link.GetAttributeValue("href", string.Empty);
                if (Uri.TryCreate(baseUrl, href, out var uri) && uri.Host == baseUrl.Host)
                {
                    links.Add(uri);
                }
            }
            return links;
        }
    }
}
