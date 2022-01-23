using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for AlcatrazSettings.xaml
    /// </summary>
    public partial class AlcatrazSettings : Page
    {
        private Manager manager;
        public Alcatraz alcatraz;
        public AlcatrazSettings(Manager _manager)
        {
            InitializeComponent();

            manager = _manager;

            ReadExistingFile();
            UpdateFields();
        }

        public void ReadExistingFile()
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;

            if(File.Exists(appFolder + @"\Alcatraz.json"))
            {
                alcatraz = JsonConvert.DeserializeObject<Alcatraz>(File.ReadAllText(appFolder + @"\Alcatraz.json"));
            }
        }

        public void UpdateFields()
        {
            if(alcatraz != null)
            {
                if (alcatraz.Profiles.Alcatraz != null)
                {
                    tbAlcUsername.Text = alcatraz.Profiles.Alcatraz.Username;
                    tbAlcAccountID.Text = alcatraz.Profiles.Alcatraz.AccountId;
                    tbAlcPassword.Password = alcatraz.Profiles.Alcatraz.Password;
                    tbAlcAccessKey.Password = alcatraz.Profiles.Alcatraz.AccessKey;
                    tbAlcServiceURL.Text = alcatraz.Profiles.Alcatraz.ServiceUrl;
                    tbAlcConfigKey.Password = alcatraz.Profiles.Alcatraz.ConfigKey;
                }

                if(alcatraz.Profiles.UbiOfficial != null)
                {
                    tbUbiUsername.Text = alcatraz.Profiles.UbiOfficial.AccountId;
                    tbUbiPassword.Password = alcatraz.Profiles.UbiOfficial.Password;
                    tbUbiGameKey.Password = alcatraz.Profiles.UbiOfficial.GameKey;
                }
                else
                {
                    Console.WriteLine("Failed to load UBISOFT account.");
                }
            }
        }

        public void SaveAlcatrazFile(Alcatraz data)
        {
            try
            {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Alcatraz.json", JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {

            }
        }

        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            Alcatraz newData = new Alcatraz();
            newData.Profiles = new AlcatrazProfile();
            newData.Profiles.Alcatraz = new AlcatrazProfileData();
            newData.Profiles.UbiOfficial = new AlcatrazProfileData();

            newData.Profiles.Alcatraz.Username = tbAlcUsername.Text;
            newData.Profiles.Alcatraz.AccountId = tbAlcAccountID.Text;
            newData.Profiles.Alcatraz.Password = tbAlcPassword.Password;
            newData.Profiles.Alcatraz.AccessKey = tbAlcAccessKey.Password;
            newData.Profiles.Alcatraz.ServiceUrl = tbAlcServiceURL.Text;
            newData.Profiles.Alcatraz.ConfigKey = tbAlcConfigKey.Password;
            newData.Profiles.UbiOfficial.AccountId = tbUbiUsername.Text;
            newData.Profiles.UbiOfficial.Password = tbUbiPassword.Password;
            newData.Profiles.UbiOfficial.GameKey = tbUbiGameKey.Password;

            switch(cbAccount.SelectedIndex)
            {
                case 0:
                    newData.UseProfile = "Alcatraz";
                    break;
                case 1:
                    newData.UseProfile = "UbiOfficial";
                    break;
            }

            SaveAlcatrazFile(newData);

            ReadExistingFile();

            UpdateFields();

            manager.frame.Content = manager.page;
            manager.alcatrazPage = null;
        }

        private void ReadFromConfig()
        {
            string[] data = File.ReadAllLines(manager.settings.ModFolder + @"\_Alcatraz\\AlcatrazLauncher\\AlcatrazLauncher.exe.config");
            foreach(string s in data)
            {
                string noSpace = Regex.Replace(s, " ", "");
                
                if(noSpace.ToUpper().StartsWith("<ADDKEY=\"SERVICEURL\"VALUE=\""))
                {
                    tbAlcServiceURL.Text = noSpace.ToUpper().Replace("<ADDKEY=\"SERVICEURL\"VALUE=\"", "").Replace("\"/>", "").ToLower();

                }
                else if(noSpace.ToUpper().StartsWith("<ADDKEY=\"CONFIGKEY\"VALUE=\""))
                {
                    tbAlcConfigKey.Password = noSpace.ToUpper().Replace("<ADDKEY=\"CONFIGKEY\"VALUE=\"", "").Replace("\"/>", "").ToLower();
                }
                else if (noSpace.ToUpper().StartsWith("<ADDKEY=\"SANDBOXACCESSKEY\"VALUE=\""))
                {
                    tbAlcAccessKey.Password = noSpace.Replace("<addkey=\"SandboxAccessKey\"value=\"", "").Replace("\"/>", "");
                }

            }
        }

        private void bDefault_Click(object sender, RoutedEventArgs e)
        {
            ReadFromConfig();
        }

        private void bLaunchAlcatraz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(manager.settings.ModFolder + @"\_Alcatraz\AlcatrazLauncher\AlcatrazLauncher.exe", "skipsearch");
            }
            catch
            {

            }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            manager.frame.Content = manager.page;
            manager.alcatrazPage = null;
        }
    }
}
