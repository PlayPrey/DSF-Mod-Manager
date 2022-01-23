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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DsfMm.Frontend
{
    /// <summary>
    /// Interaction logic for ModPreview.xaml
    /// </summary>
    public partial class ModPreview : Page
    {
        private Manager manager;
        string folderCache = "";
        public ModPreview(Manager _manager)
        {
            InitializeComponent();
            manager = _manager; 
        }

        public string ModTitle
        {
            set
            {
                lTitle.Content = value;
            }
        }

        public string ModVersion
        {
            set
            {
                lVersion.Content = value;
            }
        }

        public string ModDescription
        {
            set
            {
                lDesc.Content = value;
            }
        }

        public void PopulateScreen(string folder)
        {
            folderCache = folder;

            if(File.Exists(folder + @"\manifest.json"))
            {
                ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(folder + @"\manifest.json"));
                ModTitle = manifest.DisplayName;
                ModVersion = "Version " + manifest.Version + " by " + manifest.Dev;
                ModDescription = manifest.Description;
            }
            else
            {
                ModTitle = new DirectoryInfo(folder).Name;
                ModVersion = "Version Unknown by Unknown";
                ModDescription = "This mod has no description, it's a pity that.";
            }

            if (File.Exists(folder + @"\cache"))
            {
                bToggle.IsEnabled = true;

                InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(folder + @"\cache"));
                if (status.isEnabled)
                    bToggle.Content = "Disable Mod";
                else
                    bToggle.Content = "Enable Mod";
            }
            else
                bToggle.IsEnabled = false;
        }

        private void bBack_Click(object sender, RoutedEventArgs e)
        {
            manager.frame.Content = manager.page;
        }

        private void bToggle_Click(object sender, RoutedEventArgs e)
        {
            InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(folderCache + @"\cache"));
            status.isEnabled = !status.isEnabled;

            File.WriteAllText(folderCache + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));

            if (status.isEnabled)
                bToggle.Content = "Disable Mod";
            else bToggle.Content = "Enable Mod";
        }
    }
}
