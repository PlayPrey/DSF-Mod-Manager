using DsfMm.Frontend;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DsfMm.Scripts
{
    public static class InstallManager
    {
        public static string BOOTSTRAP_PATH = "\\Common\\UserLuaScripts\\bootstrap.lua";
        public static string BOOTSTRAPPER_INIT = "function UserBootstrapper.Init()";
        public static string BOOTSTRAPPER_LAUNCH = "function UserBootstrapper.Launch()";
        public static string BOOTSTRAPPER_SHUTDOWN = "function UserBootstrapper.Shutdown()";


        private static Settings settings
        {
            get
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.json"))
                {
                    Settings result = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.json"));
                    return result;
                }
                else
                    return null;
            }
        }
        private static string GameDir
        {
            get
            {
                if (settings != null)
                {
                    return settings.GameDirectory.Replace("Driver.exe", "");
                }
                else
                    return string.Empty;
            }
        }
        public static int Install(string mod)
        {
            try
            {
                InstallationStatus status = null;

                if (File.Exists(mod + @"\cache"))
                    status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(mod + @"\cache"));
                else
                    status = new InstallationStatus();

                status.files = new List<string>();
                status.folder = mod;

                if (status.isAlcatraz)
                {
                    if (File.Exists(mod + "\\AlcatrazLauncher\\ubiorbitapi_r2_loader.dll"))
                    {
                        // Sure, backup ain't supposed to be here but who cares...
                        if (File.Exists(GameDir + "\\ubiorbitapi_r2_loader.dll") && !File.Exists(settings.ModFolder + "\\_BackupFiles\\ubiorbitapi_r2_loader.dll"))
                            File.Move(GameDir + "\\ubiorbitapi_r2_loader.dll", settings.ModFolder + "\\_BackupFiles\\ubiorbitapi_r2_loader.dll");

                        File.Copy(mod + "\\AlcatrazLauncher\\ubiorbitapi_r2_loader.dll", GameDir + "\\ubiorbitapi_r2_loader.dll", true);
                    }
                }
                else
                {

                    foreach (string file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                    {
                        string fileId = file.ToLower().Replace(mod.ToLower(), "");
                        string pasteLocation = GameDir + fileId;

                        if (fileId.EndsWith("bootstrap.lua"))
                            continue;

                        if (!Directory.Exists(Path.GetDirectoryName(pasteLocation)))
                            Directory.CreateDirectory(Path.GetDirectoryName(pasteLocation));

                        File.Copy(file, pasteLocation, true);

                        status.files.Add(fileId);
                    }
                }

                status.isInstalled = true;

                File.WriteAllText(mod + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));

                return 0;
            }
            catch(DirectoryNotFoundException)
            {
                return 2;
            }
            catch
            {
                return 1;
            }
        }
        public static int UninstallDisabledMod(string mod)
        {
            try
            {
                InstallationStatus status = null;

                if (File.Exists(mod + @"\cache"))
                    status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(mod + @"\cache"));
                else
                    status = new InstallationStatus();

                if(status.isInstalled && !status.isEnabled)
                {
                    foreach (string file in status.files)
                    {
                        if(File.Exists(GameDir + file))
                            File.Delete(GameDir + file);


                        if (settings.VanillaFiles.Contains(file.Remove(0,1)))
                        {
                            if(File.Exists(settings.ModFolder + @"\_BackupFiles" + file))
                            {
                                File.Copy(settings.ModFolder + @"\_BackupFiles" + file, GameDir + file);
                            }
                        }
                    }

                }

                status.isInstalled = false;

                File.WriteAllText(mod + @"\cache", JsonConvert.SerializeObject(status, Formatting.Indented));

                return 0;
            }
            catch (DirectoryNotFoundException)
            {
                return 2;
            }
            catch
            {
                return 1;
            }
        }

        private static bool ModIsLuaScript(string modFolder)
        {
            if (File.Exists(modFolder + BOOTSTRAP_PATH))
                return true;
            else return false;
        }

        public static void GenerateBootstrapper()
        {
            List<string> bootstrappers = new List<string>();

            foreach(string dir in Directory.GetDirectories(settings.ModFolder))
            {
                if(!dir.EndsWith("_BackupFiles"))
                {
                    InstallationStatus status = JsonConvert.DeserializeObject<InstallationStatus>(File.ReadAllText(dir + @"\cache"));
                    if (File.Exists(dir + BOOTSTRAP_PATH) && status.isEnabled)
                    {
                        bootstrappers.Add(dir + BOOTSTRAP_PATH);
                    }

                }
            }

            int stage = 0;
            List<string> Init = new List<string>();
            List<string> Launch = new List<string>();
            List<string> Shutdown = new List<string>();

            // Gather all information required to generate a new and neat bootstrapper file.

            if(bootstrappers.Count > 0)
            {
                foreach (string file in bootstrappers)
                {
                    foreach(string line in File.ReadAllLines(file))
                    {
                        if (line.Contains(BOOTSTRAPPER_INIT) || line.Contains(BOOTSTRAPPER_LAUNCH) || line.Contains(BOOTSTRAPPER_SHUTDOWN))
                            stage++;

                        // Stage 0 = Before any function
                        // Stage 1 = Init
                        // Stage 2 = Launch
                        // Stage 3 = Shutdown
                        // Stage 4 = After all functions

                        if(line.Contains("user_open") || line.Contains("dofile"))
                        {
                            Console.WriteLine(stage + ": " + line);
                            switch(stage)
                            {
                                case 1:
                                    if(!Init.Contains(line))
                                        Init.Add(line);
                                    break;
                                case 2: 
                                    if(!Launch.Contains(line))
                                        Launch.Add(line);
                                    break;
                                case 3:
                                    if(!Shutdown.Contains(line))
                                        Shutdown.Add(line);
                                    break;
                            }
                        }

                    }
                    stage = 0;
                }
            }

            // Generate the freaking bootstrapper! 

            List<string> result = new List<string>();

            result.Add("-- This file is now managed by a mod manager, the edits you make manually to this file will be removed automatically next time you launch the game!");
            result.Add("");
            result.Add("UserBootstrapper = {}");
            result.Add("");
            result.Add(BOOTSTRAPPER_INIT);
            result.Add("\tprint(\"--------------- Bootstrapper init ---------------\")");

            if(Init.Count > 0)
                foreach(string line in Init)
                    result.Add(line);

            result.Add("end");
            result.Add("");
            result.Add(BOOTSTRAPPER_LAUNCH);
            result.Add("\tprint(\"--------------- Bootstrapper Launch scripts ---------------\")");

            if (Launch.Count > 0)
                foreach (string line in Launch)
                    result.Add(line);

            result.Add("end");
            result.Add("");
            result.Add(BOOTSTRAPPER_SHUTDOWN);
            result.Add("\tprint(\"--------------- Bootstrapper Shutdown ---------------\")");

            if (Shutdown.Count > 0)
                foreach (string line in Shutdown)
                    result.Add(line);

            result.Add("end");
            result.Add("");
            result.Add("return UserBootstrapper");
            result.Add("");
            result.Add("-- Have a nice day, from Prey. Remember, no touchie!");

            File.WriteAllLines(GameDir + @"\Common\\UserLuaScripts\\bootstrap.lua", result.ToArray());



        }
    }
}
