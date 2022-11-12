﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.UI.Wpf;

namespace Crafty;
 
public partial class MainWindow : Window
{
    public static MainWindow Current;

    private List<CraftyVersion> VersionList { get { return CraftyLauncher.VersionList; } }
    private List<CraftyVersion> FabricVersionList { get { return CraftyLauncher.FabricVersionList; } }
    private int PhysicalMemory = CraftyEssentials.GetPhysicalMemory();
  

    public MainWindow()
    {
        Current = this;
        InitializeComponent();

        CraftyConfig.loadFile();
        RamSlider.Minimum = 2048;
        RamSlider.Maximum = PhysicalMemory;
        RamSlider.TickFrequency = 2048;
        RamSlider.Value = CraftyConfig.loadRamFromJson();
        Username.Text = CraftyConfig.loadUsernameFromJson();

        CraftyLauncher.AutoLogin();
        CraftyEssentials.GetVersions();

        VersionBox.ItemsSource = VersionList;
        VersionBox.SelectedItem = VersionList.First();
    }

    private void OnExit(object sender, CancelEventArgs e) { Environment.Exit(0); }

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

            catch (LoginCancelledException)
            {
                return;
            }
        }

        else
        {
            try { CraftyLauncher.CraftyLogin.ClearCache(); } catch { Debug.WriteLine("Couldn't clear cache!"); }
            try { File.Delete($"{CraftyLauncher.CraftyPath}/crafty_session.json"); } catch { Debug.WriteLine("Couldn't delete cache file!"); }
            try { File.Delete($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Crafty.exe.WebView2/EBWebView/Local State"); } catch { Debug.WriteLine("Couldn't delete token file!"); }

            CraftyLauncher.LoggedIn = false;
            Username.IsEnabled = true;
            LoginLogout.Content = "Login";
        }
    }

    private async void PlayEvent(object sender, RoutedEventArgs e)
    {
        if (!CraftyEssentials.CheckUsername(Username.Text))
        {
            MessageBox.Show($"Wrong username!");
            return;
        }
        
        CraftyConfig.writeFile(Username.Text, RamSlider.Value);

        if (!CraftyLauncher.LoggedIn) { CraftyLauncher.Session = MSession.GetOfflineSession(Username.Text); }

        MLaunchOption LauncherOptions = new MLaunchOption
        {
            MaximumRamMb = (int)RamSlider.Value,
            Session = CraftyLauncher.Session,
        };

        Version Version = (Version)VersionBox.SelectedItem;

        if (!CraftyLauncher.VersionList.Contains(Version))
        {
            MessageBox.Show($"Version is not selected!");
            VersionBox.SelectedItem = CraftyLauncher.VersionList.First();
            return; 
        }

        Username.IsEnabled = false;
        LoginLogout.IsEnabled = false;
        VersionBox.IsEnabled = false;
        Play.IsEnabled = false;
        Settings.IsEnabled = false;

        if (Version.isOriginal)
        {
            try
            {
                // DownloadText.Text = "Downloading Java";
                // await CraftyEssentials.DownloadJava();
                // Old versions crash on Java 19 - using Minecraft's default Java runtimes for now

                DownloadText.Text = $"Downloading {Version.id}.jar";
                await CraftyEssentials.DownloadVersion(Version.id);

                DownloadText.Text = $"Downloading {Version.id}.json";
                await CraftyEssentials.DownloadJson(Version.id);

                DownloadText.Text = "Fetching assets";
                await CraftyEssentials.DownloadAssets(Version.id);

                DownloadText.Text = "Fetching libraries";
                await CraftyEssentials.DownloadLibraries(Version.id);
            }

            catch
            {
                DownloadText.Text = "Failed to download files while using efficient method - using normal method";
                await CraftyLauncher.Launcher.CheckAndDownloadAsync(await CraftyLauncher.Launcher.GetVersionAsync(Version.id));
            }
        }

        DownloadText.Text = $"Downloading missing files (this might take a while)";
        var Minecraft = await CraftyLauncher.Launcher.CreateProcessAsync(Version.id, LauncherOptions, true);
        Minecraft.Start();
        UpdateVersionBox();
        DownloadText.Text = $"Launched Minecraft {Version.id}";

        await Task.Delay(3000);
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

    private void UpdateVersionBox()
    {
        int SelectedIndex = VersionBox.SelectedIndex;
        VersionBox.SelectedIndex = -1;
        VersionBox.SelectedIndex = SelectedIndex;
        VersionBox.Items.Refresh();
    }

    private void RamSliderEvent(object sender, RoutedPropertyChangedEventArgs<double> e) { RamText.Text = e.NewValue.ToString(); }

    private void SnapshotChecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetSnapshots = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void SnapshotUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetSnapshots = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void BetaChecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetBetas = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void BetaUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetBetas = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void AlphaChecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetAlphas = true;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    private void AlphaUnchecked(object sender, RoutedEventArgs e)
    {
        CraftyLauncher.GetAlphas = false;
        CraftyEssentials.GetVersions();
        UpdateVersionBox();
    }

    public void ChangeDownloadText(string s) { Dispatcher.Invoke(new Action(() => DownloadText.Text = s)); }
}
