using Downloader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crafty;

public class DialogContent
{
    public string Title { get; set; }
    public string Description { get; set; }

    public DialogContent(string title, string description)
    {
        Title = title;
        Description = description;
    }
}

public static class CraftyEssentials
{
    public static readonly string CraftyVersion = "v1.0";
    private static readonly string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_1234567890";
    private static readonly string VersionManifest = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
    private static readonly int MaxTasks = 128;
    private static readonly DownloadConfiguration DownloadConfig = new()
    {
        ChunkCount = 8,
        MaxTryAgainOnFailover = 5,
        ParallelDownload = true,
        ParallelCount = 4,
        Timeout = 1000,
    };

    private static string RandomString(int length) { return new string(Enumerable.Repeat(AllowedChars, length).Select(s => s[new Random().Next(s.Length)]).ToArray()); }

    public static bool CheckUsername(string username)
    {
        foreach (char unvalid in username) { if (!AllowedChars.Contains(unvalid.ToString())) { return false; } }
        if (username.Length < 3 || username.Length > 16 || string.IsNullOrEmpty(username)) { return false; }

        return true;
    }

    public static void GetVersions()
    {
        CraftyLauncher.VersionList.Clear();

        var response = new RestClient(VersionManifest).Execute(new RestRequest());
        var json = JObject.Parse(response.Content);
        json.Remove("latest");
        var versions = json.Values().Children();

        foreach (var version in versions)
        {
            string id = (string)version["id"];
            string type = (string)version["type"];

            if (type == "release")
            {
                CraftyLauncher.VersionList.Add(new CraftyVersion(id, id, type, true));
                Debug.WriteLine($"Added {type} {id}");
            }

            else if (type == "snapshot" && CraftyConfig.Data.GetSnapshots)
            {
                CraftyLauncher.VersionList.Add(new CraftyVersion(id, id, type, true));
                Debug.WriteLine($"Added {type} {id}");
            }

            else if (type == "old_beta" && CraftyConfig.Data.GetBetas)
            {
                CraftyLauncher.VersionList.Add(new CraftyVersion(id, id, type, true));
                Debug.WriteLine($"Added {type} {id}");
            }

            else if (type == "old_alpha" && CraftyConfig.Data.GetAlphas)
            {
                CraftyLauncher.VersionList.Add(new CraftyVersion(id, id, type, true));
                Debug.WriteLine($"Added {type} {id}");
            }
        }

        foreach (var item in CraftyLauncher.Launcher.GetAllVersions())
        {
            if (item.IsLocalVersion && !CraftyLauncher.VersionList.Any(x => x.Name == item.Name))
            {
                CraftyLauncher.VersionList.Add(new CraftyVersion($"{item.Name} (Installed)", item.Name, item.Type));
                Debug.WriteLine($"Added already installed {item.Name}");
            }

            else if (item.IsLocalVersion && CraftyLauncher.VersionList.Any(x => x.Name == item.Name))
            {
                var index = CraftyLauncher.VersionList.FindIndex(x => x.Name == item.Name);
                var editVersion = CraftyLauncher.VersionList.Find(x => x.Name == item.Name);
                editVersion.Name = $"{item.Name} (Installed)";
                CraftyLauncher.VersionList[index] = editVersion;
                Debug.WriteLine($"Changed status of {item.Name} to installed");
            }
        }
    }

    public static async Task DownloadVersion(string version)
    {
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/versions/{version}");
        string path = $"{CraftyLauncher.CraftyPath}/versions/{version}/{version}.jar";
        if (File.Exists(path)) { return; }

        string website = new WebClient().DownloadString($"https://mcversions.net/download/{version}");

        foreach (LinkItem item in LinkFinder.Find(website))
        {
            Debug.WriteLine(item.Href);
            if (item.Text == "Download Client Jar")
            {
                DownloadService downloader = new(DownloadConfig);
                await downloader.DownloadFileTaskAsync(item.Href, path);

                var index = CraftyLauncher.VersionList.FindIndex(x => x.Name == version);
                var editVersion = CraftyLauncher.VersionList.Find(x => x.Name == version);
                editVersion.Name = $"{version} (Installed)";
                CraftyLauncher.VersionList[index] = editVersion;
                Debug.WriteLine($"Changed status of {version} to installed");

                return;
            }
        }
    }

    public static async Task DownloadJson(string version)
    {
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/versions/{version}");
        string path = $"{CraftyLauncher.CraftyPath}/versions/{version}/{version}.json";
        if (File.Exists(path)) { return; }

        string website = new WebClient().DownloadString($"https://minecraft.fandom.com/wiki/Java_Edition_{version}");

        foreach (LinkItem item in LinkFinder.Find(website))
        {
            if (item.Text == ".json")
            {
                DownloadService downloader = new(DownloadConfig);
                await downloader.DownloadFileTaskAsync(item.Href, path);

                return;
            }
        }
    }

    public static async Task DownloadJava()
    {
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/temp");
        string tempPath = $"{CraftyLauncher.CraftyPath}/temp/{RandomString(10)}.zip";
        if (File.Exists($"{CraftyLauncher.JavaPath}/bin/javaw.exe")) { return; }

        DownloadService downloader = new(DownloadConfig);
        await downloader.DownloadFileTaskAsync("https://cdn.azul.com/zulu/bin/zulu19.30.11-ca-jre19.0.1-win_x64.zip", tempPath);

        await Task.Run(() =>
        {
            string javaVersion;

            using (ZipArchive zip = ZipFile.Open(tempPath, ZipArchiveMode.Update))
            {
                zip.ExtractToDirectory($"{CraftyLauncher.CraftyPath}/temp/");
                javaVersion = zip.Entries.First().ToString();
            }

            if (Directory.Exists(CraftyLauncher.JavaPath)) { Directory.Delete(CraftyLauncher.JavaPath); }
            Directory.Move($"{CraftyLauncher.CraftyPath}/temp/{javaVersion}", CraftyLauncher.JavaPath);

            ClearTemp();
        });
    }

    private static void ClearTemp()
    {
        DirectoryInfo tempPath = new($"{CraftyLauncher.CraftyPath}/temp");
        foreach (FileInfo file in tempPath.GetFiles()) { file.Delete(); }
        foreach (DirectoryInfo directory in tempPath.GetDirectories()) { directory.Delete(true); }
    }

    public static async Task DownloadAssets(string version)
    {
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/assets/indexes");
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/assets/objects");

        string jsonUrl = GetPackageUrl(version);
        string jonId = GetPackageId(version);
        string jonPath = $"{CraftyLauncher.CraftyPath}/assets/indexes/{jonId}.json";

        if (!File.Exists(jonPath)) {
            DownloadService IndexDownloader = new(DownloadConfig);
            await IndexDownloader.DownloadFileTaskAsync(jsonUrl, jonPath);
        }

        StreamReader read = new(jonPath);
        var json = JObject.Parse(read.ReadToEnd()).Values();
        read.Close();
        var assets = json.Children().ToArray();
        int remaining = assets.Length;
        int done = 0;
        int tasks = 0;

        foreach (var jsonObject in assets)
        {
            foreach (var jsonObjectInfo in jsonObject)
            {
                string hash = (string)jsonObjectInfo["hash"];
                int size = (int)jsonObjectInfo["size"];
                string shortHash = hash.Substring(0, 2);
                string objectPath = $"{CraftyLauncher.CraftyPath}/assets/objects/{shortHash}";

                FileInfo hashFile = new($"{objectPath}/{hash}");
                if (hashFile.Exists && hashFile.Length == size) { remaining--; }
            }
        }

        foreach (var jsonObject in assets)
        {
            foreach (var jsonObjectInfo in jsonObject)
            {
                while (tasks > MaxTasks) { await Task.Delay(1000); }
                tasks++;

                string hash = (string)jsonObjectInfo["hash"];
                int size = (int)jsonObjectInfo["size"];
                string shortHash = hash.Substring(0, 2);
                string objectPath = $"{CraftyLauncher.CraftyPath}/assets/objects/{shortHash}";
                string hashPath = $"{objectPath}/{hash}";
                string url = $"http://resources.download.minecraft.net/{shortHash}/{hash}";

                FileInfo HashFile = new($"{objectPath}/{hash}");
                if (HashFile.Exists)
                { 
                    if (HashFile.Length != size)
                    { 
                        try { HashFile.Delete(); }

                        catch (IOException)
                        {
                            tasks--;
                            continue;
                        }
                    }

                    else
                    {
                        tasks--;
                        continue; 
                    }
                }

                Action downloadAction = async () =>
                {
                    Directory.CreateDirectory(objectPath);
                    DownloadService downloader = new(DownloadConfig);
                    await downloader.DownloadFileTaskAsync(url, hashPath);
                    done++;
                    MainWindow.Current.ChangeDownloadText($"Downloading assets ({done}/{remaining})");
                    tasks--;
                };

                Task downloadThread = new(downloadAction);
                downloadThread.Start();
            }
        }

        while (tasks > 0) { await Task.Delay(1000); }
    }

    public static async Task DownloadLibraries(string version)
    {
        Directory.CreateDirectory($"{CraftyLauncher.CraftyPath}/libraries");

        string jsonPath = $"{CraftyLauncher.CraftyPath}/versions/{version}/{version}.json";
        StreamReader read = new(jsonPath);
        JsonTextReader reader = new(read);
        JObject json = (JObject)JToken.ReadFrom(reader);
        read.Close();
        int remaining = json["libraries"].Count();
        int done = 0;
        int tasks = 0;

        foreach (var jsonObject in json["libraries"])
        {
            string libraryPath = $"{CraftyLauncher.CraftyPath}/libraries/{jsonObject["downloads"].SelectTokens("$..path").Last()}";
            int size = (int)jsonObject["downloads"].SelectTokens("$..size").Last();

            FileInfo libraryFile = new(libraryPath);
            if (libraryFile.Exists && libraryFile.Length == size) { remaining--; }
        }

        foreach (var jsonObject in json["libraries"])
        {
            while (tasks > MaxTasks) { await Task.Delay(1000); }
            tasks++;

            string libraryPath = $"{CraftyLauncher.CraftyPath}/libraries/{jsonObject["downloads"].SelectTokens("$..path").Last()}";
            string libraryFolderPath = Path.GetDirectoryName(libraryPath);
            int size = (int)jsonObject["downloads"].SelectTokens("$..size").Last();
            string url = $"https://libraries.minecraft.net/{libraryPath}";

            FileInfo libraryFile = new(libraryPath);
            if (libraryFile.Exists)
            {
                if (libraryFile.Length != size)
                { 
                    try { libraryFile.Delete(); }

                    catch (IOException)
                    {
                        tasks--;
                        continue;
                    }
                }

                else
                {
                    tasks--;
                    continue;
                }
            }

            Action downloadAction = async () =>
            {
                Directory.CreateDirectory(libraryFolderPath);
                DownloadService downloader = new(DownloadConfig);
                await downloader.DownloadFileTaskAsync(url, libraryPath);
                done++;
                MainWindow.Current.ChangeDownloadText($"Downloading libraries ({done}/{remaining})");
                tasks--;
            };

            Task downloadThread = new(downloadAction);
            downloadThread.Start();
        }

        while (tasks > 0) { await Task.Delay(1000); }
    }

    private static string? GetPackageUrl(string version)
    {
        string jsonPath = $"{CraftyLauncher.CraftyPath}/versions/{version}/{version}.json";
        StreamReader read = new(jsonPath);
        JsonTextReader reader = new(read);
        JObject json = (JObject)JToken.ReadFrom(reader);
        read.Close();

        return (string?)json["assetIndex"]["url"];
    }

    private static string? GetPackageId(string version)
    {
        string jsonPath = $"{CraftyLauncher.CraftyPath}/versions/{version}/{version}.json";
        StreamReader read = new(jsonPath);
        JsonTextReader reader = new(read);
        JObject json = (JObject)JToken.ReadFrom(reader);
        read.Close();

        return (string?)json["assetIndex"]["id"];
    }

    public static int GetPhysicalMemory()
    {
        decimal installedMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        int physicalMemory = (int)Math.Round(installedMemory / 1048576);
        Debug.WriteLine($"Physical Memory: {physicalMemory}MB");

        return physicalMemory;
    }
}
