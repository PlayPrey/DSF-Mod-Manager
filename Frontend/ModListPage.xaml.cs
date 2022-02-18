using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Newtonsoft.Json;
using FontFamily = System.Windows.Media.FontFamily;

namespace DsfMm.Frontend
{
    /// <summary>
    /// Interaction logic for ModListPage.xaml
    /// </summary>
    public partial class ModListPage : Page
    {
        private Manager manager;
        public Dictionary<string, CheckBox> listCheckboxes;
        public Dictionary<string, Label> listLabels;
        public Dictionary<string, ModManifest> listMods;
        public Dictionary<string, string> listModFolders;
        public List<string> mods;
        public ModListPage(Manager _manager)
        {
            InitializeComponent();

            manager = _manager;

            InitializeOther();

            primaryScrollview.Children.Clear();

            LoadModsFolder();

            DropModToInstall.Height = 0;
        }

        private void LoadModsFolder()
        {
            string id_result = String.Empty;

            try
            {
                string[] modsFolders = Directory.GetDirectories(manager.settings.ModFolder);
                foreach (string modFolder in modsFolders)
                {
                    if (modFolder.ToLower().EndsWith("_backupfiles"))
                        continue;


                    //First load manifest to make it look gooder ;) 
                    if(ReadModManifest(modFolder) != null)
                    {
                        ModManifest manifest = ReadModManifest(modFolder);
                        AddListing(manifest.Identifier, manifest.DisplayName);
                        id_result = manifest.Identifier.ToUpper();
                        listMods.Add(id_result, manifest);
                    }
                    else
                    {

                        id_result = new DirectoryInfo(modFolder).Name.ToLower().Replace(" ", "").ToUpper();
                        AddListing(id_result, new DirectoryInfo(modFolder).Name);
                    }

                    listModFolders.Add(id_result, modFolder);

                    if(ReadModCache(modFolder) != null)
                    {
                        InstallationStatus status = ReadModCache(modFolder);
                        if (status.isInstalled)
                            listCheckboxes[id_result].IsChecked = true;

                        if(id_result == "ALCATRAZ")
                        {
                            if (status.Priority != -100)
                                status.Priority = -100;

                            File.WriteAllText(modFolder + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));
                        }
                    }
                    else
                    {
                        ScanModFolder(modFolder, true);
                    }



                    mods.Add(modFolder);

                }


                ReOrderListAccordingToPriority();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Source + " reported an error when working with " + id_result + "!\n\n" +  ex.Message + "\n\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        public void ReOrderListAccordingToPriority()
        {
            primaryScrollview.Children.Clear();

            List<InstallationStatus> allCacheFiles = new List<InstallationStatus>();

            foreach(string mod in mods )
            {
                InstallationStatus cache = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(mod + @"\cache"));
                allCacheFiles.Add(cache);
            }

            allCacheFiles = allCacheFiles.OrderBy(o => o.Priority).ToList();

            listCheckboxes.Clear();
            listLabels.Clear();
            //listModFolders.Clear();

            foreach(InstallationStatus cache in allCacheFiles)
            {
                if(File.Exists(cache.folder + @"\manifest.json"))
                {
                    ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(cache.folder + @"\manifest.json"));
                    AddListing(manifest.Identifier.ToUpper(), manifest.DisplayName);

                    if (cache.isInstalled)
                        listCheckboxes[manifest.Identifier.ToUpper()].IsChecked = true;
                }
                else
                    AddListing(new DirectoryInfo(cache.folder).Name.ToUpper().Replace(" ", ""), new DirectoryInfo(cache.folder).Name);
            }
        }
        public void ScanModFolder(string folder, bool assumeNew)
        {
            InstallationStatus status = new InstallationStatus();

            List<string> files = new List<string>();

            foreach(string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (!file.ToLower().EndsWith("readme.txt") && !file.ToLower().EndsWith("changelog.txt"))
                {
                    files.Add(file.Replace(folder, ""));
                }
            }

            status.files = files;
            
            if(assumeNew)
            {
                status.isInstalled = false;
                status.isEnabled = true;
                status.Priority = mods.Count;
                status.folder = folder;
            }
            else
            {

            }

            File.WriteAllText(folder + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));
        }

        public InstallationStatus ReadModCache(string folder)
        {
            if (File.Exists(folder + @"\cache"))
            {
                InstallationStatus status = null;

                try
                {
                    status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(folder + @"\cache"));

                    return status;
                }
                catch
                {
                    MessageBox.Show("Error occured while reading cache files...");
                    return null;
                }

            }
            else return null;
        }
        public void SaveModCache(string modFolder, InstallationStatus cache)
        {

        }

        public ModManifest ReadModManifest(string folder)
        {
            if (File.Exists(folder + @"\cache"))
            {
                ModManifest manifest = null;

                try
                {
                    manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(folder + @"\manifest.json"));

                    return manifest;
                }
                catch
                {
                    return null;
                }

            }
            else return null;
        }

        public void InitializeOther()
        {
            listCheckboxes = new Dictionary<string, CheckBox>(); 
            listLabels = new Dictionary<string, Label>();
            listMods = new Dictionary<string, ModManifest>();
            listModFolders = new Dictionary<string, string>();
            mods = new List<string>();
        }

        public void AddListing(string modId, string displayName, bool onTop = false)
        {
            if (!listCheckboxes.ContainsKey(modId) && !listLabels.ContainsKey(modId))
            {
                string MODID = modId.ToUpper();

                Button newButton = new Button();
                Grid buttonGrid = new Grid();
                newButton.Content = buttonGrid;

                //Setup Button Properties
                newButton.HorizontalAlignment = HorizontalAlignment.Left;
                newButton.VerticalAlignment = VerticalAlignment.Top;
                newButton.Height = 40;
                newButton.Width = 784;
                newButton.BorderBrush = null;
                newButton.Foreground = null;
                newButton.Margin = new Thickness(0);
                ImageBrush ib = new ImageBrush();
                ib.ImageSource = Imaging.CreateBitmapSourceFromBitmap(Properties.Resources.list_hover);
                newButton.Background = ib;

                // Setup Button Grid
                buttonGrid.Width = 784;
                buttonGrid.Height = 40;

                //Setup Checkbox
                CheckBox modCheckbox = new CheckBox();
                buttonGrid.Children.Add(modCheckbox);
                modCheckbox.HorizontalAlignment = HorizontalAlignment.Left;
                modCheckbox.VerticalAlignment = VerticalAlignment.Center;
                modCheckbox.Margin = new Thickness(4, -10, 0, 0);
                ScaleTransform checkboxScale = new ScaleTransform(1.6, 1.6);
                modCheckbox.RenderTransform = checkboxScale;
                modCheckbox.IsEnabled = false;
                listCheckboxes.Add(MODID, modCheckbox);

                //Setup Label
                Label text = new Label();
                buttonGrid.Children.Add(text);
                text.FontFamily = (FontFamily)FindResource("Agency");
                text.Content = displayName;
                text.Margin = new Thickness(40, 0, 0, 0);
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Height = 40;
                text.VerticalContentAlignment = VerticalAlignment.Center;
                text.FontSize = 18;

                newButton.Click += delegate(object sender , RoutedEventArgs e) { NewButton_Click(sender, e, MODID); };

                if (onTop)
                    primaryScrollview.Children.Insert(0, newButton);
                else
                    primaryScrollview.Children.Add(newButton);

                manager.console.AddLog("Added Mod List ID: " + modId);

                Console.WriteLine("Succesfully added listing with Mod ID: " + modId + " and DisplayName " + displayName);
            }
            else
            {
                MessageBox.Show("Tried to add another mod with the same id as another!\n\nConflicting ID: " + modId, "Warning", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
        private ModScanResult VerifyArchive(string path)
        {
            ModScanResult result = new ModScanResult();
            // first check for a manifest
            try
            {
                ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(path + "\\manifest.json"));
                result.hasManifest = true;
            }
            catch
            {
                Console.WriteLine("No manifest, such sad...");
            }

            //Then, just have a peek for familiar folders and such

            if (Directory.Exists(path + "\\bin"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\Common"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\Input"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\Install"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\Locale"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\media"))
                result.hasFamiliarStructure = true;

            if (Directory.Exists(path + "\\plugins"))
                result.hasFamiliarStructure = true;

            return result;
        }

        private bool IsAlcatraz(string folder)
        {
            try
            {
                string[] files = Directory.GetFiles(folder + @"\AlcatrazLauncher");
                bool ExeLauncher = false;
                bool OrbitApi = false;

                foreach(string file in files)
                {
                    if(file.EndsWith("AlcatrazLauncher.exe"))
                        ExeLauncher = true;
                    else if(file.EndsWith("ubiorbitapi_r2_loader.dll"))
                        OrbitApi = true;
                }

                if (ExeLauncher && OrbitApi)
                    return true;
                else return false;
            }
            catch
            {
                return false;
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e, string modID)
        {
            if (modID == "ALCATRAZ")
            {
                if (manager.alcatrazPage == null) manager.alcatrazPage = new AlcatrazSettings(manager);
                manager.frame.Content = manager.alcatrazPage;
            }
            else
            {
                try
                {

                    //MessageBox.Show("Settings are not yet available for: " + modID);
                    if (manager.modPreview == null) manager.modPreview = new ModPreview(manager);
                    manager.frame.Content = manager.modPreview;
                    manager.modPreview.PopulateScreen(listModFolders[modID.ToUpper()]);
                }
                catch(System.Collections.Generic.KeyNotFoundException)
                {
                    MessageBox.Show("An error related to the MOD ID detection occured, please re-launch the software.\nChecked for mod id: " + modID.ToUpper());
                }
            }
        }

        private void TryReadAlcatraz()
        {
            Alcatraz alcatraz = JsonConvert.DeserializeObject<Alcatraz>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Alcatraz.json"));
            MessageBox.Show(alcatraz.Profiles.UbiOfficial.GameKey);
        }

        private void DropModToInstall_DragEnter(object sender, DragEventArgs e)
        {
            DropModToInstall.Height = double.NaN;
        }

        private void DropModToInstall_DragLeave(object sender, DragEventArgs e)
        {

            DropModToInstall.Height = 0;
        }

        private void primaryGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

                ScanDroppedFiles(droppedFiles);
            }
        }

        private bool IsModLikelyInstalled(string modFolder)
        {
            return false;
        }

        public void ScanDroppedFiles(string[] files)
        {
            foreach (string file in files)
            {
                if (file.ToLower().EndsWith(".zip"))
                {
                    ZipArchive archive = ZipFile.Open(file, ZipArchiveMode.Read);

                    string ExtractionPath = manager.settings.ModFolder + "\\" + Path.GetFileNameWithoutExtension(file);

                    Console.WriteLine("Scanning folder: " + ExtractionPath);
                    manager.console.AddLog("Scanning folder: " + ExtractionPath);

                    if (!Directory.Exists(ExtractionPath))
                        archive.ExtractToDirectory(ExtractionPath);
                    else
                    {
                        MessageBox.Show("Folder required for extraction already exsists, skipping.");
                        continue;
                    }



                    if (VerifyArchive(ExtractionPath).hasManifest)
                    {
                        // Work with a manifested mod
                        ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(ExtractionPath + @"\manifest.json");

                        AddListing(manifest.Identifier.ToUpper(), manifest.DisplayName);

                        listModFolders.Add(manifest.Identifier.ToUpper(), ExtractionPath);
                        listCheckboxes[manifest.Identifier.ToUpper()].IsChecked = true;
                    }
                    else if (VerifyArchive(ExtractionPath).hasFamiliarStructure)
                    {

                        //Mod mod = new Mod() { Dev = "Unknown", Name = (manager.settings.ModFolder + "\\" + Path.GetFileNameWithoutExtension(file)).Split(Path.DirectorySeparatorChar).Last(), Version = "1.0" };
                        string modID = Path.GetFileNameWithoutExtension(file).ToUpper().Replace(" ", "");

                        AddListing(modID, Path.GetFileNameWithoutExtension(file));

                        listModFolders.Add(modID, ExtractionPath);
                        listCheckboxes[modID].IsChecked = true;

                        //MessageBox.Show(modID);
                    }
                    else
                    {
                        ////////////
                        /// Alcatraz Launcher Found
                        if (IsAlcatraz(ExtractionPath))
                        {
                            Directory.Move(ExtractionPath, manager.settings.ModFolder + @"\_Alcatraz");
                            InstallationStatus status = new InstallationStatus();
                            status.Priority = -100;
                            status.isAlcatraz = true;
                            status.files = new List<string>() { "\\AlcatrazLauncher\\AlcatrazLauncher.exe", "\\AlcatrazLauncher\\ubiorbitapi_r2_loader.dll" };
                            status.folder = manager.settings.ModFolder + @"\_Alcatraz";

                            File.WriteAllText(manager.settings.ModFolder + @"\_Alcatraz\cache", JsonConvert.SerializeObject(status, Formatting.Indented));

                            ModManifest manifest = new ModManifest();
                            manifest.Version = "Unknown";
                            manifest.Identifier = "alcatraz";
                            manifest.Dev = "Soapy";
                            manifest.DisplayName = "Alcatraz";

                            File.WriteAllText(manager.settings.ModFolder + @"\_Alcatraz\manifest.json", JsonConvert.SerializeObject(manifest, Formatting.None));

                            AddListing(manifest.Identifier, manifest.DisplayName, true);

                        }
                        else
                        {
                            MessageBoxResult result = MessageBox.Show("This archive don't quite look right, and I can't recommend installing it as a DSF Mod.\nWould to like to do it anyways?", "What's up?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {

                            }
                            else
                            {
                                try
                                {
                                    Directory.Delete(ExtractionPath, true);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("At the moment only .zip archives are supported.", "Sorry not sorry?", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    continue;
                }
            }



            DropModToInstall.Height = 0;
        }
    }

    public static class Imaging
    {
        public static System.Windows.Media.Imaging.BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
    }

    
}
