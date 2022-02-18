using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using DsfMm.Scripts;
using Microsoft.Win32;

namespace DsfMm.Frontend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Manager : Window
    {
        public ConsoleWindow console;
        public Settings settings;
        public ModListPage page;
        public AlcatrazSettings alcatrazPage;
        public ModPreview modPreview;

        private bool debug = false;
        private bool hasIncorrectVersion;
        private string executableHash;
        public Manager()
        {
            InitializeComponent();

            console = new ConsoleWindow();

            while(!LoadSettings())


            console.AddLog("== Console Initialized ==");
            

            page = new ModListPage(this);
            frame.Content = page;

            textDsfVer.Text = ConvertHashToVersion(DsfHash);
            console.AddLog("== Game Version: "+ConvertHashToVersion(DsfHash)+" ==");


            RefreshCheckboxStatus();
        }

        public string DsfHash
        {
            get
            {
                try
                {
                    MD5 md5 = MD5.Create();
                    using (FileStream stream = File.OpenRead(settings.GameDirectory))
                    {
                        byte[] hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash);
                    }
                }
                catch
                {
                    MessageBox.Show("An error occured while loading the settings.json file, resetting.");
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "Settings.json");
                    Application.Current.Shutdown();
                    return string.Empty;
                }
            }
        }


        public bool LoadSettings()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Settings.json"))
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Settings.json"));
                console.AddLog("== Loading Settings File ==");

                // Make sure that the modfolder value isn't zero, and if it is apply a default one. 
                if (settings.ModFolder == null || settings.ModFolder == string.Empty)
                {
                    settings.ModFolder = AppDomain.CurrentDomain.BaseDirectory + "\\Mods";

                    if(!Directory.Exists(settings.ModFolder))
                        Directory.CreateDirectory(settings.ModFolder);

                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "Settings.json", JsonConvert.SerializeObject(settings, Formatting.None));
                }

                if (settings.ModListing == null)
                    settings.ModListing = new List<Mod>();


                console.AddLog("== Settings File Loaded ==");
                return true;
            }
            else
            {
                console.AddLog("Settings file missing, running setup!");
                Setup();
                return false;
            }
        }
        public void Setup()
        {
            Welcome w = new Welcome();
            w.ShowDialog();
        }
        public string ConvertHashToVersion(string hash)
        {
            switch(hash)
            {
                case "B1-DA-F4-38-3B-39-44-A1-B2-0F-36-86-15-92-08-92":
                    return "Driver San Fransisco 1.04.1114";

                default:
                    hasIncorrectVersion = true;
                    executableHash = hash;
                    return "Unknown Game Version";
            }
        }

        public void Launch(bool withMods, bool withAlcatraz)
        {
            if (withMods)
            {
                List<string> modFolderToInstall = new List<string>();
                foreach (string dir in Directory.GetDirectories(settings.ModFolder + "\\"))
                {
                    if (dir.EndsWith("_BackupFiles"))
                        continue;

                    modFolderToInstall.Add(dir);
                    console.AddLog("Added Mod Folder to Install Check List: " + dir);
                }

                if (debug)
                    MessageBox.Show("Now installing" + modFolderToInstall[0]);

                List<string> directoriesToScan = new List<string>();
                List<string> filesToBackup = new List<string>();

                if (!Directory.Exists(settings.ModFolder + "\\_BackupFiles"))
                {
                    Directory.CreateDirectory(settings.ModFolder + "\\_BackupFiles");
                    console.AddLog("_BackupFiles folder was missing, but should be created now");
                }

                // Initial folder scan
                for (int modFolder = 0; modFolder < modFolderToInstall.Count; modFolder++)
                {
                    foreach (string folder in Directory.GetDirectories(modFolderToInstall[modFolder]))
                    {

                        if (!directoriesToScan.Contains(folder))
                        {
                            directoriesToScan.Add(folder);
                        }
                    }

                    bool stillHasNewFolders = true;

                    //////////////////////////////
                    /// Get Every single folder that could contain data that needs to be backed up
                    /// 
                    while (stillHasNewFolders)
                    {
                        stillHasNewFolders = false;

                        for (int i = 0; i < directoriesToScan.Count; i++)
                        {
                            foreach (string newfolder in Directory.GetDirectories(directoriesToScan[i]))
                            {
                                if (!directoriesToScan.Contains(newfolder))
                                {
                                    directoriesToScan.Add(newfolder);
                                    stillHasNewFolders = true;
                                }
                            }
                        }
                    }

                    /////////////////////////////////
                    /// Next function is 100% responsible for dealing with finding files that exists and then backing them up.

                    BackupFilesModWillReplace(modFolderToInstall[modFolder], settings.GameDirectory.Replace(@"\Driver.exe", ""));

                }


                console.AddLog("Stage 1 complete, now injecting mods into the game");

                // Now inject the mods into the game
                for (int modFolder = 0; modFolder < modFolderToInstall.Count; modFolder++)
                {
                    bool shouldInstall = true;
                    bool shouldRemove = false;
                    // Check first real quick for install status
                    if (File.Exists(modFolderToInstall[modFolder] + @"\cache"))
                    {
                        InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(modFolderToInstall[modFolder] + @"\cache"));

                        if (status.isInstalled && !status.isEnabled)
                        {
                            shouldInstall = false;
                            shouldRemove = true;
                            console.AddLog(modFolderToInstall[modFolder] + " is flagged for removal");
                        }
                        else if (status.isInstalled || !status.isEnabled)
                        {
                            shouldInstall = false;
                            console.AddLog(modFolderToInstall[modFolder] + " is flagged for skipping install (" + status.isInstalled + "|" + status.isEnabled + ")");
                        }
                        else
                        {
                            console.AddLog(modFolderToInstall[modFolder] + " is flagged for install-now");
                        }
                    }
                    //else
                    //    MessageBox.Show(modFolderToInstall[modFolder] + @"\cache Don' exis!");

                    if (shouldInstall)
                    {
                        switch (InstallManager.Install(modFolderToInstall[modFolder]))
                        {
                            case 0:
                                console.AddLog("Successfully installed " + modFolder);
                                break;
                            case 1:
                                MessageBox.Show("Unknown error occured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            case 2:
                                MessageBox.Show("Game directory is invalid, maybe settings file is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                    else if(shouldRemove)
                    {
                        InstallManager.UninstallDisabledMod(modFolderToInstall[modFolder]);

                        console.AddLog("Successfully removed " + modFolder);
                    }
                }

                InstallManager.GenerateBootstrapper();

                console.AddLog("Game is ready to launch!");

                MessageBox.Show("Starting game...");

                Directory.SetCurrentDirectory(settings.GameDirectory.Replace("Driver.exe", ""));
                Process game = Process.Start(settings.GameDirectory);

                game.EnableRaisingEvents = true;
                game.Exited += Game_Exited;

            }
            else
            {
                Process.Start(settings.GameDirectory);
            }

        }

        private void BackupFilesModWillReplace(string mod, string game)
        {
            if(debug)
                MessageBox.Show("Will start checking from mod;\n" + mod + "\n\nFrom game;\n" + game);

            foreach(string file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
            {
                // Don't check redundant files
                if (file.ToLower().EndsWith("readme.txt") || file.ToLower().EndsWith("manifest.json"))
                    continue;

                //MessageBox.Show("check " + file);

                string fileId = file.ToLower().Replace(mod.ToLower(), "");
                string fileToTest = game + fileId;
                string backupDir = settings.ModFolder + @"\_BackupFiles";

                string pasteLocation = backupDir + fileId;

                //MessageBox.Show("Comparing two files:\n" + fileId + "\n" + @"\" + settings.VanillaFiles[35]);


                if (File.Exists(fileToTest) && settings.VanillaFiles.Contains(fileId.Remove(0, 1).ToLower()))
                {
                    if (debug)
                        MessageBox.Show("Found file\n\n" + fileToTest + "\n\nI want to copy this to:\n" + backupDir + fileId);

                    console.AddLog("Backing up file: " + fileToTest);

                    if (!Directory.Exists(Path.GetDirectoryName(pasteLocation)))
                        Directory.CreateDirectory(Path.GetDirectoryName(pasteLocation));

                    if(!File.Exists(pasteLocation))
                        File.Copy(fileToTest, pasteLocation, true);


                    console.AddLog("Success!");
                }


            }

            if(debug)
                MessageBox.Show("DONE WITH BACKUP");
            console.AddLog("Done with backup process!");
        }

        public void RefreshCheckboxStatus()
        {
            string lastId = "";
            try
            {
                foreach(string modFolder in Directory.GetDirectories(settings.ModFolder))
                {
                    if (modFolder.EndsWith("_BackupFiles"))
                        continue;

                    string id = "";

                    if(File.Exists(modFolder + @"\manifest.json"))
                    {
                        ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(modFolder + @"\manifest.json"));
                        id = manifest.Identifier.ToUpper();
                        lastId = id;
                    }
                    else
                    {
                        id = new DirectoryInfo(modFolder).Name.Replace(" ", "").ToUpper();
                        lastId = id;
                    }

                    InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(modFolder + @"\cache"));

                    if (status.isEnabled)
                    {
                        page.listCheckboxes[id].IsChecked = true;
                        console.AddLog("Should check the box for id: " + id);
                    }
                    else
                    {
                        page.listCheckboxes[id].IsChecked = false;
                        console.AddLog("Should uncheck the box for id: " + id);
                    }
                }
            }
            catch(Exception ex)
            {
                console.AddLog(ex.Message);
            }
        }


        //private void InstallModsIntoGame(string mod, string game)
        //{
        //    InstallationStatus status = null;

        //    if (File.Exists(mod + @"\cache"))
        //        status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(mod + @"\cache"));
        //    else
        //        status = new InstallationStatus();

        //    status.files = new List<string>();
        //    status.folder = mod;

        //    if (status.isAlcatraz)
        //    {
        //        if(File.Exists(mod + "\\AlcatrazLauncher\\ubiorbitapi_r2_loader.dll"))
        //        {
        //            // Sure, backup ain't supposed to be here but who cares...
        //            if (File.Exists(game + "\\ubiorbitapi_r2_loader.dll") && !File.Exists(settings.ModFolder + "\\_BackupFiles\\ubiorbitapi_r2_loader.dll"))
        //                File.Move(game + "\\ubiorbitapi_r2_loader.dll", settings.ModFolder + "\\_BackupFiles\\ubiorbitapi_r2_loader.dll");

        //            File.Copy(mod + "\\AlcatrazLauncher\\ubiorbitapi_r2_loader.dll", game + "\\ubiorbitapi_r2_loader.dll", true);
        //        }
        //    }
        //    else
        //    {

        //        foreach (string file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
        //        {
        //            string fileId = file.ToLower().Replace(mod.ToLower(), "");
        //            string pasteLocation = game + fileId;

        //            if (!Directory.Exists(Path.GetDirectoryName(pasteLocation)))
        //                Directory.CreateDirectory(Path.GetDirectoryName(pasteLocation));

        //            File.Copy(file, pasteLocation, true);

        //            status.files.Add(fileId);
        //        }
        //    }

        //    try
        //    {
        //        if (status.isAlcatraz)
        //            page.listCheckboxes["ALCATRAZ"].IsChecked = true;
        //        else
        //            page.listCheckboxes[new DirectoryInfo(status.folder).Name.ToUpper().Replace(" ", "")].IsChecked = true;
        //    }
        //    catch
        //    {
        //        Console.WriteLine("===================================");
        //        Console.WriteLine("Failed to check the checkbox for mod.");
        //        Console.WriteLine("===================================");
        //    }

        //    status.isInstalled = true;

        //    File.WriteAllText(mod + @"\cache" ,JsonConvert.SerializeObject(status, Formatting.Indented));
        //}


        // Dedicated to make sure the files within seems relevant, and check if there is a manifest file to make the experience A LOT BETTER. (not really)
        // Is being moved to ModListPage who is now responsable of all of this.
        private ModScanResult VerifyArchive(string path)
        {
            ModScanResult result = new ModScanResult();
            // first check for a manifest
            try
            {
                ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(path + "\\manifest.json");
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

        // Should be done? 
        private void ReturnGameToVanilla()
        {
            /// Uninstall all custom files from the game
            foreach(string mod in Directory.GetDirectories(settings.ModFolder))
            {
                if (mod.EndsWith("_BackupFiles"))
                    continue;

                if(File.Exists(mod + @"\cache"))
                {
                    InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(mod + @"\cache"));

                    if (status.isInstalled)
                    {
                        if (status.files != null)
                        {
                            if (status.files.Count > 0)
                            {
                                foreach (string file in status.files)
                                {
                                    if (debug)
                                        MessageBox.Show("Delete file: \n" + settings.GameDirectory.Replace(@"\Driver.exe", "") + file);

                                    if (File.Exists(settings.GameDirectory.Replace(@"\Driver.exe", "") + file))
                                        File.Delete(settings.GameDirectory.Replace(@"\Driver.exe", "") + file);
                                }
                            }
                        }
                    }

                    if(status.isAlcatraz)
                    {
                        // ALCATRAZ DELETION
                        File.Delete(settings.GameDirectory.Replace(@"\Driver.exe", @"\ubiorbitapi_r2_loader.dll"));
                        File.Copy(settings.ModFolder + @"\_BackupFiles\\ubiorbitapi_r2_loader.dll", settings.GameDirectory.Replace(@"\Driver.exe", @"\ubiorbitapi_r2_loader.dll"));
                    }

                    status.isInstalled = false;

                    File.WriteAllText(mod + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));
                }
                else
                    MessageBox.Show("Error during uninstallation of mod\n\n" + mod);

            }
            // Check for foreign files
            foreach(string gameFile in Directory.GetFiles(settings.GameDirectory.Replace("Driver.exe",""), "*", SearchOption.AllDirectories))
            {
                if (settings.VanillaFiles.Contains(gameFile.Replace(settings.GameDirectory.Replace("Driver.exe", ""), "").ToLower()))
                {
                    // All is good with this file yes yes :) 
                }
                else
                {
                    File.Delete(gameFile);
                }
            }


            if (Directory.Exists(settings.ModFolder + @"\_BackupFiles"))
            {
                //Put all backup files back in place
                foreach (string backupFile in Directory.GetFiles(settings.ModFolder + @"\_BackupFiles", "*", SearchOption.AllDirectories))
                {
                    string fileId = backupFile.Replace(settings.ModFolder + @"\_BackupFiles", "");
                    string game = settings.GameDirectory.Replace("Driver.exe", "");

                    if (debug)
                        MessageBox.Show(game + fileId);

                    File.Copy(backupFile, game + fileId, true);
                }
            }


            Process.Start(settings.GameDirectory);
        }















































        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Event handlers


        private void Game_Exited(object sender, EventArgs e)
        {
            if(debug)
                MessageBox.Show("Hope you had fun... " + Environment.UserName);
        }

        private void statusBarVersion_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(hasIncorrectVersion)
            {
                MessageBox.Show("The version is unknown, please send a screenshot of this message a long with an explanation of what version you have to the mod manager author.\n\n" + executableHash, "Unknown Version Detected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void bPlayWithMods_Click(object sender, RoutedEventArgs e)
        {
            Launch(true, false);
        }

        private void modListView_Drop(object sender, DragEventArgs e)
        {
            
        }

        private void bInstallMods_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

                page.ScanDroppedFiles(droppedFiles);
            }

            bInstallModLabel.Content = "Install new Mod";
        }

        private void bInstallMods_DragEnter(object sender, DragEventArgs e)
        {
            bInstallModLabel.Content = "Drop Here";
        }

        private void bInstallMods_DragLeave(object sender, DragEventArgs e)
        {
            bInstallModLabel.Content = "Install new Mod";
        }

        private void bGoToVanilla_Click(object sender, RoutedEventArgs e)
        {
            ReturnGameToVanilla();
        }

        private void App_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.Width = 800;
        }

        private void bCreateManifest_Click(object sender, RoutedEventArgs e)
        {
            GenerateManifest gen = new GenerateManifest();
            gen.ShowDialog();
        }

        private void bInstallMods_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Zip Archive (*.zip)|*.zip";

            if(open.ShowDialog() == true)
            {
                page.ScanDroppedFiles(open.FileNames);
            }
        }

        private void bDebug_Click(object sender, RoutedEventArgs e)
        {
            console.Show();
        }
    }
    public class Settings
    {
        public string GameDirectory { get; set; }
        public string ModFolder { get; set; }
        public List<Mod> ModListing { get; set; }
        public List<string> VanillaFiles { get; set; }
    }
    public class ModScanResult
    {
        public bool hasManifest { get; set; }
        public bool hasFamiliarStructure { get; set; }
    }
    public class ModManifest
    {
        public string Identifier { get; set; }
        public string Dev { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
    }
    public class Mod
    {
        public string Name { get; set; }
        public string Dev { get; set; }
        public string Version { get; set; }
    }

    public class InstallationStatus
    {
        public List<string> files;
        public string folder;
        public bool isInstalled;
        public bool isEnabled;
        public int Priority;
        public bool isAlcatraz;
    }
    public class Alcatraz
    {
        public string UseProfile;
        public AlcatrazProfile Profiles;
    }
    public class AlcatrazProfile
    {
        public AlcatrazProfileData Alcatraz;
        public AlcatrazProfileData UbiOfficial;
    }
    public class AlcatrazProfileData
    {
        public string Username;
        public string AccountId;
        public string Password;
        public string ServiceUrl;
        public string ConfigKey;
        public string AccessKey;
        public string GameKey;
    }
}
