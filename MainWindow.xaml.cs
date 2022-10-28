﻿using System.Threading.Tasks;
using System.Windows;

namespace Crafty;

public partial class MainWindow : Window
{
    public static MainWindow Current;

    public MainWindow()
    {
        Current = this;
        InitializeComponent();
        CraftyEssentials.GetVersions();
        VersionList.SelectedItem = CraftyEssentials.LatestVersion;
    }

    public async void PlayEvent(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Username.Text) || !CraftyEssentials.CheckUsername(Username.Text))
        {
            MessageBox.Show($"Wrong username!");
            return;
        }

        Username.IsEnabled = false;
        VersionList.IsEnabled = false;
        Play.IsEnabled = false;

        DownloadText.Text = "Downloading Java...";
        await CraftyEssentials.DownloadJava();

        DownloadText.Text = "Downloading Jar...";
        await CraftyEssentials.DownloadVersion((string)VersionList.SelectedItem);

        DownloadText.Text = "Downloading Json...";
        await CraftyEssentials.DownloadJson((string)VersionList.SelectedItem);

        DownloadText.Text = "Downloading Assets...";
        await CraftyEssentials.DownloadAssets((string)VersionList.SelectedItem);

        DownloadText.Text = "Downloading Libraries...";
        await CraftyEssentials.DownloadLibraries((string)VersionList.SelectedItem);

        DownloadText.Text = $"Launching Minecraft {VersionList.SelectedItem}...";
        Username.IsEnabled = true;
        VersionList.IsEnabled = true;
        Play.IsEnabled = true;
    }

    public async void TestEvent(object sender, RoutedEventArgs e)
    {
        await CraftyEssentials.DownloadLibraries((string)VersionList.SelectedItem);
    }

    public async void ChangeTitle(string title)
    {
        Title = title;
    }

    public async void AddVersion(string version)
    {
        VersionList.Items.Add(version);
    }

    public async Task ChangeDownloadText(string s)
    {
        DownloadText.Text = s;
    }
}
