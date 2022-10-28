using CmlLib.Core;
using CmlLib.Core.Auth;
using Downloader;
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

public static class CraftyEssentials
{
    public static string CraftyPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/.crafty";
    public static string CraftyVersion = "0.1";
    public static string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_123456789";
    public static string VersionManifest = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
    public static string LatestVersion = null;
    public static DownloadConfiguration DownloadConfig = new DownloadConfiguration()
    {
        ChunkCount = 8,
        MaxTryAgainOnFailover = 5,
        ParallelDownload = true,
        ParallelCount = 4,
        Timeout = 1000,
    };
    private static Random random = new Random();

    public static string RandomString(int length)
    {
        return new string(Enumerable.Repeat(AllowedChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static bool CheckUsername(string username)
    {
        foreach (char unvalid in username)
        {
            if (!AllowedChars.Contains(unvalid.ToString()))
                return false;
        }

        return true;
    }

    public static void GetVersions()
    {
        var Client = new RestClient(VersionManifest);
        var Request = new RestRequest();
        var Response = Client.Execute(Request);

        var Json = JObject.Parse(Response.Content);
        Json.Remove("latest");
        var VersionList = Json.Values();

        foreach (var Versions in VersionList)
        {
            foreach (var Version in Versions)
            {
                string id = (string)Version["id"];
                string type = (string)Version["type"];
                string url = (string)Version["url"];
                string time = (string)Version["time"];
                string releaseTime = (string)Version["releaseTime"];

                if (type == "release")
                {
                    MainWindow.Current.AddVersion(id);
                    Debug.WriteLine($"Added {id}");

                    if (LatestVersion == null)
                    {
                        LatestVersion = id;
                    }
                }
            }
        }
    }

    public static async Task DownloadVersion(string version)
    {
        Directory.CreateDirectory($"{CraftyPath}/versions/{version}");
        string Path = $"{CraftyPath}/versions/{version}/{version}.jar";
        if (File.Exists(Path)) { return; }

        WebClient Client = new WebClient();
        string Website = Client.DownloadString($"https://mcversions.net/download/{version}");

        foreach (LinkItem Item in LinkFinder.Find(Website))
        {
            if (Item.Text == "Download Client Jar")
            {
                var Downloader = new DownloadService(DownloadConfig);
                await Downloader.DownloadFileTaskAsync(Item.Href, Path);

                return;
            }
        }
    }

    public static async Task DownloadJson(string version)
    {
        string CraftyPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/.crafty";
        Directory.CreateDirectory($"{CraftyPath}/versions/{version}");
        string Path = $"{CraftyPath}/versions/{version}/{version}.json";
        if (File.Exists(Path)) { return; }

        WebClient Client = new WebClient();
        string Website = Client.DownloadString($"https://minecraft.fandom.com/wiki/Java_Edition_{version}");

        foreach (LinkItem Item in LinkFinder.Find(Website))
        {
            if (Item.Text == ".json")
            {
                var Downloader = new DownloadService(DownloadConfig);
                await Downloader.DownloadFileTaskAsync(Item.Href, Path);

                return;
            }
        }
    }

    public static async Task DownloadJava()
    {
        string CraftyPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/.crafty";
        Directory.CreateDirectory($"{CraftyPath}/temp");
        string TempPath = $"{CraftyPath}/temp/{RandomString(10)}.zip";
        if (File.Exists(TempPath)) { return; }

        var Downloader = new DownloadService(DownloadConfig);
        await Downloader.DownloadFileTaskAsync($"https://download.oracle.com/java/19/latest/jdk-19_windows-x64_bin.zip", TempPath);
        if (File.Exists($"{CraftyPath}/java")) return;

        Directory.CreateDirectory($"{CraftyPath}/java");
        await Task.Run(() => ZipFile.ExtractToDirectory(TempPath, $"{CraftyPath}/java"));
        await Task.Run(() => File.Delete(TempPath));
    }
}

public class CraftyMinecraftPath : MinecraftPath
{
    public CraftyMinecraftPath(string path)
    {
        BasePath = NormalizePath(path);

        Library = NormalizePath(BasePath + "/libraries");
        Versions = NormalizePath(BasePath + "/versions");
        Resource = NormalizePath(BasePath + "/resources");

        Runtime = NormalizePath(BasePath + "/java");
        Assets = NormalizePath(BasePath + "/assets");

        CreateDirs();
    }
}