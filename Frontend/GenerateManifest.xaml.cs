using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DsfMm.Frontend
{
    /// <summary>
    /// Interaction logic for GenerateManifest.xaml
    /// </summary>
    public partial class GenerateManifest : Window
    {
        public GenerateManifest()
        {
            InitializeComponent();
        }

        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            ModManifest manifest = new ModManifest();
            manifest.DisplayName = tbDisplayName.Text;
            manifest.Version = tbVersion.Text;
            manifest.Dev = tbAuthor.Text;
            manifest.Description = tbDesc.Text;
            manifest.Identifier = tbAuthor.Text.Replace(" ", "") + "." + tbDisplayName.Text.Replace(" ", "");

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON (*.json)|*.json";
            dialog.FileName = "manifest";
            dialog.AddExtension = true;
            dialog.DefaultExt = "json";
           

            if(dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            }
        }
    }
}
