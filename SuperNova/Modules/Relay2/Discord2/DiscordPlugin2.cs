/*
    Copyright 2015 SuperNova
        
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
using System.IO;
using SuperNova.Config;
using SuperNova.Events.ServerEvents;
using SuperNova.Modules.Relay2.Discord2;

namespace SuperNova.Modules.Relay2.Discord2 
{
    public sealed class Discord2Config 
    {
        [ConfigBool("enabled", null, false)]
        public bool Enabled;
        [ConfigString("bot-token", null, "", true)]
        public string BotToken = "";
        [ConfigString("status-message", null, "with {PLAYERS} players")]
        public string StatusMessage = "with {PLAYERS} players";
        [ConfigBool("use-nicknames", null, true)]
        public bool UseNicks = true;
        
        [ConfigString("channel-ids", null, "", true)]
        public string Channels = "";
        [ConfigString("op-channel-ids", null, "", true)]
        public string OpChannels = "";
        [ConfigString("ignored-user-ids", null, "", true)]
        public string IgnoredUsers = "";
        
        [ConfigBool("presence-enabled", null, true)]
        public bool PresenceEnabled = true;
        [ConfigEnum("presence-status", null, PresenceStatus.online, typeof(PresenceStatus))]
        public PresenceStatus Status = PresenceStatus.online;        
        [ConfigEnum("presence-activity", null, PresenceActivity.Playing, typeof(PresenceActivity))]
        public PresenceActivity Activity = PresenceActivity.Playing;
        
        [ConfigInt("embed-color", null, 9758051)]
        public int EmbedColor = 9758051;
        
        [ConfigBool("can-mention-users", null, true)]
        public bool CanMentionUsers = true;
        [ConfigBool("can-mention-roles", null, true)]
        public bool CanMentionRoles = true;
        [ConfigBool("can-mention-everyone", null, false)]
        public bool CanMentionHere;
        
        const string PROPS_PATH = "properties/discordbot2.properties";
        static ConfigElement[] cfg;
        
        public void Load() {
            // create default config file
            if (!File.Exists(PROPS_PATH)) Save();

            if (cfg == null) cfg = ConfigElement.GetAll(typeof(Discord2Config));
            ConfigElement.ParseFile(cfg, PROPS_PATH, this);
        }
        
        public void Save() {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(Discord2Config));
            
            using (StreamWriter w = new StreamWriter(PROPS_PATH)) {
                w.WriteLine("# Discord relay bot configuration");
                w.WriteLine("# See " + Updater.SourceURL + "/wiki/Discord-relay-bot/");
                w.WriteLine();
                ConfigElement.SerialiseElements(cfg, w, this);
            }
        }
    }
    
    public enum PresenceStatus { online, dnd, idle, invisible }
    public enum PresenceActivity { Playing = 0, Listening = 2, Watching = 3, Competing = 5 }
    
    public sealed class DiscordPlugin2 : Plugin 
    {
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string SuperNova_Version { get { return Server.Version; } }
        public override string name { get { return "DiscordRelay2"; } }
        
        public static Discord2Config Config = new Discord2Config();
        public static DiscordBot2 Bot2 = new DiscordBot2();
        
        public override void Load(bool startup) {
            Bot2.Config = Config;
            Bot2.ReloadConfig();
            Bot2.Connect();
            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
            Bot2.Disconnect("Disconnecting Discord2 bot");
        }
        
        void OnConfigUpdated() { Bot2.ReloadConfig(); }
    }
    
    public sealed class CmdDiscordBot2 : RelayBotCmd2 
    {
        public override string name { get { return "DiscordBot2"; } }
        public override RelayBot2 Bot { get { return DiscordPlugin2.Bot2; } }
    }
    
    public sealed class CmdDiscordControllers2 : BotControllersCmd2 
    {
        public override string name { get { return "DiscordControllers2"; } }
        public override RelayBot2 Bot { get { return DiscordPlugin2.Bot2; } }
    }
}
