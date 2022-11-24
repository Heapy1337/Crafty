using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth;
using CmlLib.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using CmlLib.Core.Auth.Microsoft.Cache;

namespace Crafty;

public class CraftyVersion
{
    public string? Name { get; set; }
    public string? Id { get; set; }
    public string? Type { get; set; }
    public bool IsOriginal { get; set; }

    public CraftyVersion(string? name, string? id, string? type, bool isOriginal = false)
    {
        Name = name;
        Id = id; 
        Type = type;
        IsOriginal = isOriginal;
    }
}

public class CraftyLauncher
{
    public static readonly string CraftyPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/.crafty";
    public static readonly string JavaPath = $"{CraftyPath}/java";
    public static readonly CMLauncher Launcher = new(new MinecraftPath(CraftyPath));
    public static readonly List<CraftyVersion> VersionList = new();
    public static bool LoggedIn = false;
    public static MSession Session;
    public static readonly LoginHandler CraftyLogin = new(x => x.CacheManager = new(new JsonFileCacheManager<SessionCache>($"{CraftyPath}/session.json")));

    public static void AutoLogin()
    {
        try
        {
            Session = CraftyLogin.LoginFromCache().Result;
            LoggedIn = true;
            MainWindow.Current.Username.IsEnabled = false;
            MainWindow.Current.Username.Text = Session.Username;
            MainWindow.Current.LoginLogout.Content = "Logout";
        }

        catch { Debug.WriteLine("Couldn't auto login!"); }
    }
}

