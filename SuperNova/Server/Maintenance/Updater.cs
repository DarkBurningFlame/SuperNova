/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/SuperNova)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using SuperNova.Network;
using SuperNova.Tasks;

namespace SuperNova {
    /// <summary> Checks for and applies software updates. </summary>
    public static class Updater {
        
        public static string SourceURL = "https://github.com/RandomStrangers/SuperNova/";
        public const string BaseURL    = "https://github.com/RandomStrangers/SuperNova/blob/master/";
        public const string UploadsURL = "https://github.com/RandomStrangers/SuperNova/tree/master/Uploads";
        
        const string CurrentVersionURL = BaseURL + "Uploads/current_version.txt";
        #if TEN_BIT_BLOCKS
        const string dllURL = BaseURL + "Uploads/SuperNova_infid.dll?raw=true";
        #else
        const string dllURL = BaseURL + "Uploads/SuperNova_.dll?raw=true";
        #endif
        const string changelogURL = BaseURL + "Changelog.txt";
        const string guiURL = BaseURL + "Uploads/SuperNova.exe?raw=true";
        const string cliURL = BaseURL + "Uploads/SuperNovaCLI.exe?raw=true";

        public static event EventHandler NewerVersionDetected;
        
        public static void UpdaterTask(SchedulerTask task) {
            UpdateCheck();
            task.Delay = TimeSpan.FromHours(2);
        }

        static void UpdateCheck() {
            if (!Server.Config.CheckForUpdates) return;
            WebClient client = HttpUtil.CreateWebClient();

            try {
                string latest = client.DownloadString(CurrentVersionURL);
                
                if (new Version(Server.Version) >= new Version(latest)) {
                    Logger.Log(LogType.SystemActivity, "No update found!");
                } else if (NewerVersionDetected != null) {
                    NewerVersionDetected(null, EventArgs.Empty);
                }
            } catch (Exception ex) {
                Logger.LogError("Error checking for updates", ex);
            }
            
            client.Dispose();
        }

        public static void PerformUpdate() {
            try {
                try {
                    DeleteFiles("Changelog.txt", "SuperNova_.update", "SuperNova.update", "SuperNovaCLI.update",
                                "prev_SuperNova_.dll", "prev_SuperNova.exe", "prev_SuperNovaCLI.exe");
                } catch {
                }
                
                WebClient client = HttpUtil.CreateWebClient();
                client.DownloadFile(dllURL, "SuperNova_.update");
                client.DownloadFile(guiURL, "SuperNova.update");
                client.DownloadFile(cliURL, "SuperNovaCLI.update");
                client.DownloadFile(changelogURL, "Changelog.txt");

                Level[] levels = LevelInfo.Loaded.Items;
                foreach (Level lvl in levels) {
                    if (!lvl.SaveChanges) continue;
                    lvl.Save();
                    lvl.SaveBlockDBChanges();
                }

                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players) pl.SaveStats();
                
                // Move current files to previous files (by moving instead of copying, 
                //  can overwrite original the files without breaking the server)
                AtomicIO.TryMove("SuperNova_.dll", "prev_SuperNova_.dll");
                AtomicIO.TryMove("SuperNova.exe", "prev_SuperNova.exe");
                AtomicIO.TryMove("SuperNovaCLI.exe", "prev_SuperNovaCLI.exe");
                
                // Move update files to current files
                File.Move("SuperNova_.update",   "SuperNova_.dll");
                File.Move("SuperNova.update",    "SuperNova.exe");
                File.Move("SuperNovaCLI.update", "SuperNovaCLI.exe");                             

                Server.Stop(true, "Updating server.");
            } catch (Exception ex) {
                Logger.LogError("Error performing update", ex);
            }
        }
        
        static void DeleteFiles(params string[] paths) {
            foreach (string path in paths) { AtomicIO.TryDelete(path); }
        }
    }
}
