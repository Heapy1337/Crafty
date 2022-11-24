using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.UI.Wpf;
using MaterialDesignThemes.Wpf;

namespace Crafty;
 
public partial class MainWindow : Window
{
    public static MainWindow Current;

    public MainWindow()
    {
        Current = this;
        InitializeComponent();

        CraftyConfig.LoadFile();
        CraftyLauncher.AutoLogin();
        CraftyEssentials.GetVersions();
     
        RamSlider.Minimum = 2048;
        RamSlider.Maximum = CraftyEssentials.GetPhysicalMemory();
        RamSlider.TickFrequency = 2048;

        VersionBox.ItemsSource = CraftyLauncher.VersionList;
        VersionBox.SelectedItem = CraftyLauncher.VersionList.First();
    }

    private void OnExit(object sender, CancelEventArgs e)
    {
        CraftyConfig.WriteFile();
        Environment.Exit(0);
    }

    private async void LoginLogoutEvent(object sender, RoutedEventArgs e)
    {
        if (!CraftyLauncher.LoggedIn)
        {
            try
            {
                MicrosoftLoginWindow LoginWindow = new(CraftyLauncher.CraftyLogin)
                {
                    Width = 500,
                    MinWidth = 500,
                    MaxWidth = 500,
                    Height = 500,
                    MinHeight = 500,
                    MaxHeight = 500,
                    Title = Title
                };
                MSession LoginSession = await LoginWindow.ShowLoginDialog();

                CraftyLauncher.Session = LoginSession;
                CraftyLauncher.LoggedIn = true;
                Username.IsEnabled = false;
                Username.Text = LoginSession.Username;
                LoginLogout.Content = "Logout";
            }

            catch (LoginCancelledException) { return; }
        }

        else
        {
            try { CraftyLauncher.CraftyLogin.ClearCache(); } catch { Debug.WriteLine("Couldn't clear cache!"); }
            try { File.Delete($"{CraftyLauncher.CraftyPath}/session.json"); } catch { Debug.WriteLine("Couldn't delete cache file!"); }
            try { File.Delete($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Crafty.exe.WebView2/EBWebView/Local State"); } catch { Debug.WriteLine("Couldn't delete token file!"); }

            CraftyLauncher.LoggedIn = false;
            Username.IsEnabled = true;
            LoginLogout.Content = "Login";
        }
    }

    private async void PlayEvent(object sender, RoutedEventArgs e)
    {
        CraftyVersion Version = (CraftyVersion)VersionBox.SelectedItem;

        if (!CraftyLauncher.VersionList.Contains(Version))
        {
            await ShowErrorDialog("Version is not selected!");
            VersionBox.SelectedItem = CraftyLauncher.VersionList.First();
            return;
        }

        if (!CraftyEssentials.CheckUsername(Username.Text))
        {
            await ShowErrorDialog("Wrong username!");
            return;
        }
        
        if (!CraftyLauncher.LoggedIn) { CraftyLauncher.Session = MSession.GetOfflineSession(Username.Text); }

        CraftyConfig.WriteFile();

        MLaunchOption LauncherOptions = new MLaunchOption
        {
            MaximumRamMb = (int)RamSlider.Value,
            Session = CraftyLauncher.Session,
            JVMArguments = JvmArguments.Text.Split(" ")
        };

        Username.IsEnabled = false;
        LoginLogout.IsEnabled = false;
        VersionBox.IsEnabled = false;
        Play.IsEnabled = false;
        Settings.IsEnabled = false;

        if (Version.IsOriginal)
        {
            try
            {
                // DownloadText.Text = "Downloading Java";
                // await CraftyEssentials.DownloadJava();
                // Old versions crash on Java 19 - using Minecraft's default Java runtimes for now

                DownloadText.Text = $"Downloading {Version.Id}.jar";
                await CraftyEssentials.DownloadVersion(Version.Id);

                DownloadText.Text = $"Downloading {Version.Id}.json";
                await CraftyEssentials.DownloadJson(Version.Id);

                DownloadText.Text = "Fetching assets";
                await CraftyEssentials.DownloadAssets(Version.Id);

                DownloadText.Text = "Fetching libraries";
                await CraftyEssentials.DownloadLibraries(Version.Id);
            }

            catch
            {
                DownloadText.Text = "Failed to download files while using efficient method - using normal method";
                await CraftyLauncher.Launcher.CheckAndDownloadAsync(await CraftyLauncher.Launcher.GetVersionAsync(Version.Id));
            }
        }

        DownloadText.Text = $"Downloading missing files (this might take a while)";
        try
        {
            var Minecraft = await CraftyLauncher.Launcher.CreateProcessAsync(Version.Id, LauncherOptions, true);
            Minecraft.Start();
            UpdateVersionBox();
            DownloadText.Text = $"Launched Minecraft {Version.Id}";

            await Task.Delay(3000);
        }
        catch
        {
            Debug.WriteLine($"Couldn't run {Version.Id}!");
            await ShowErrorDialog($"Couldn't run {Version.Id}!");
        }

        DownloadText.Text = "Crafty by heapy & Badder1337";
        if (!CraftyLauncher.LoggedIn)
        {
            Username.IsEnabled = true;
        }
        LoginLogout.IsEnabled = true;
        VersionBox.IsEnabled = true;
        Play.IsEnabled = true;
        Settings.IsEnabled = true;
    }

    private async Task ShowErrorDialog(string description)
    {
        var DialogContent = new DialogContent("Error", description);
        await DialogHost.Show(DialogContent, "RootDialog");
    }

    private void UpdateVersionBox()
    {
        int SelectedIndex = VersionBox.SelectedIndex;
        VersionBox.SelectedIndex = -1;
        VersionBox.SelectedIndex = SelectedIndex;
        VersionBox.Items.Refresh();
    }

    private void SnapshotChecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetSnapshots = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void SnapshotUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetSnapshots = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void BetaChecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetBetas = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void BetaUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetBetas = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void AlphaChecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetAlphas = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void AlphaUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyConfig.Data.GetAlphas = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void RamSliderEvent(object sender, RoutedPropertyChangedEventArgs<double> e) { RamText.Text = e.NewValue.ToString(); }

    public void ChangeDownloadText(string s) { Dispatcher.Invoke(new Action(() => DownloadText.Text = s)); }
}
