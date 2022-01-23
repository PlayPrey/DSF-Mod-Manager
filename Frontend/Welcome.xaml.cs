using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DsfMm.Frontend
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : Window
    {
        public Welcome()
        {
            InitializeComponent();
        }

        private void tbDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if(File.Exists(tbDir.Text) && tbDir.Text.ToLower().EndsWith("driver.exe"))
                {
                    bApply.IsEnabled = true;
                }
                else
                {
                    bApply.IsEnabled = false;
                }
            }
            catch
            {
                bApply.IsEnabled = false;
            }
        }

        private void bBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDriver = new OpenFileDialog();
            openDriver.FileName = "Driver.exe";
            openDriver.Filter = "Executables|*.exe";
            if (openDriver.ShowDialog() == true)
                tbDir.Text = openDriver.FileName;
        }

        private void bApply_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings { GameDirectory = tbDir.Text };

            settings.VanillaFiles = new List<string>();

            foreach(string file in Directory.GetFiles(tbDir.Text.Replace("Driver.exe", ""), "*", SearchOption.AllDirectories))
                settings.VanillaFiles.Add(file.Replace(tbDir.Text.Replace("Driver.exe", ""), "").ToLower());

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "Settings.json", JsonConvert.SerializeObject(settings));

            MessageBox.Show("It's set! Please close the program and relaunch it. This will be improved in the next update of course.");
        }


    }
}
public static class CheckKnownModWithoutManifest
{
    public static string CheckDev
    {
        get
        {
            return string.Empty;
        }
    }
}
