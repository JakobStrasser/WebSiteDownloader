using HtmlAgilityPack;
using System;
using System.IO;

namespace RecursiveWebDownloader
{
    public static class FileHelper
    {
       
        public static string GetRelativePath(Uri root, Uri url)
        {
            return root.MakeRelativeUri(url).ToString();
        }

        public static void EnsureDirectoryCreated(string directoryPath)
        {


            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }


            if (Directory.Exists(directoryPath))
            {
                return;
            }
            var dir = Path.GetDirectoryName(directoryPath);
            if (!Path.EndsInDirectorySeparator(directoryPath) && dir is not null)
            {
                Directory.CreateDirectory(dir);
                return;
            }
          
            Directory.CreateDirectory(directoryPath);

        }


        public static void EnsureTargetDirectoryIsEmpty(string directoryPath)
        {

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be null or empty", directoryPath);
            }

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
            }
            foreach (var directory in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
