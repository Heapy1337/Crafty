using Newtonsoft.Json;
using System.IO;

namespace Crafty;

public static class CraftyConfig
{
    public static void WriteFile()
    {
        Data.Username = MainWindow.Current.Username.Text;
        Data.Ram = (int)MainWindow.Current.RamSlider.Value;
        Data.JvmArguments = MainWindow.Current.JvmArguments.Text;
        var json = JsonConvert.SerializeObject(Data);
        File.WriteAllTextAsync($"{CraftyLauncher.CraftyPath}/config.json", json);
    }

    public static void LoadFile()
    {
        if (File.Exists($"{CraftyLauncher.CraftyPath}/config.json"))
        {
            string jsonString = File.ReadAllText($"{CraftyLauncher.CraftyPath}/config.json");
            JsonConvert.PopulateObject(jsonString, Data);

            MainWindow.Current.Username.Text = Data.Username;
            MainWindow.Current.RamSlider.Value = (double)Data.Ram;
            MainWindow.Current.JvmArguments.Text = Data.JvmArguments;

            if (Data.GetSnapshots) MainWindow.Current.GetSnapshots.IsChecked = true; else MainWindow.Current.GetSnapshots.IsChecked = false;
            if (Data.GetAlphas) MainWindow.Current.GetBetas.IsChecked = true; else MainWindow.Current.GetBetas.IsChecked = false;
            if (Data.GetBetas) MainWindow.Current.GetAlphas.IsChecked = true; else MainWindow.Current.GetAlphas.IsChecked = false;
        }
    }

    public class CraftyData
    {
        public string? Username { get; set; }
        public string? JvmArguments { get; set; }
        public int? Ram = 2048;
        public bool GetSnapshots { get; set; }
        public bool GetAlphas { get; set; }
        public bool GetBetas { get; set; }
    }

    public static readonly CraftyData Data = new();
}
