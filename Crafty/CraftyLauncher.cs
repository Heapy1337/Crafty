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
    public string name { get; set; }
    public string id { get; set; }
    public string type { get; set; }
    public bool isOriginal { get; set; }

    public CraftyVersion(string Name, string Id, string Type, bool IsOriginal = false)
    {
        name = Name;
        id = Id;
        type = Type;
        isOriginal = IsOriginal;
    }
}

public class CraftyLauncher
{
    public static string CraftyPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/.crafty";
    public static string JavaPath = $"{CraftyPath}/java";
    public static CMLauncher Launcher = new(new MinecraftPath(CraftyPath));
    public static List<CraftyVersion> VersionList = new();
    public static List<CraftyVersion> FabricVersionList = new();
    public static bool LoggedIn = false;
    public static MSession Session;
    public static LoginHandler CraftyLogin = new(x => x.CacheManager = new(new JsonFileCacheManager<SessionCache>($"{CraftyPath}/crafty_session.json")));

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

