using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Crafty;

public static class CraftyConfig
{
    public static void writeFile()
    {
        data.username = MainWindow.Current.Username.Text;
        data.ram = (int)MainWindow.Current.RamSlider.Value;
        var json = JsonConvert.SerializeObject(data);
        File.WriteAllTextAsync(CraftyLauncher.CraftyPath + "/config.json", json);
    }

    public static void loadFile()
    {
        if (File.Exists(CraftyLauncher.CraftyPath + "/config.json"))
        {
            string jsonString = File.ReadAllText(CraftyLauncher.CraftyPath + "/config.json");
            JsonConvert.PopulateObject(jsonString, data);

            MainWindow.Current.Username.Text = data.username;
            MainWindow.Current.RamSlider.Value = (double)data.ram;

            if (data.getSnapshots) MainWindow.Current.ToggleButton_GetSnapshots.IsChecked = true; else MainWindow.Current.ToggleButton_GetSnapshots.IsChecked = false;
            if (data.getBetas) MainWindow.Current.ToggleButton_GetBetas.IsChecked = true; else MainWindow.Current.ToggleButton_GetBetas.IsChecked = false;
            if (data.getAlphas) MainWindow.Current.ToggleButton_GetAlphas.IsChecked = true; else MainWindow.Current.ToggleButton_GetAlphas.IsChecked = false;
        }
    }

    public class Data_t
    {
        public string? username { get; set; }
        public int? ram = 2048;
        public bool getSnapshots { get; set; }
        public bool getAlphas { get; set; }
        public bool getBetas { get; set; }
    }

    public static Data_t data = new Data_t { };
}
