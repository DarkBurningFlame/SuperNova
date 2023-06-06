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
using System.Windows.Forms;
using SuperNova.Modules.Relay1.Discord1;
using SuperNova.Modules.Relay1.IRC1;
using SuperNova.SQL;

namespace SuperNova.Gui {

    public partial class PropertyWindow : Form { 
        
        void LoadRelayProps1() {
            LoadIRCProps1();
            LoadDiscordProps1();
        }
        
        void ApplyRelayProps1() {
            ApplyIRCProps1();
            ApplyDiscordProps1();
        }
        
        
        void LoadIRCProps1() {
            irc_chkEnabled.Checked = Server.Config.UseIRC1;
            irc_txtServer.Text = Server.Config.IRCServer1;
            irc_numPort.Value = Server.Config.IRCPort1;
            irc_txtNick.Text = Server.Config.IRCNick1;
            irc_txtChannel.Text = Server.Config.IRCChannels1;
            irc_txtOpChannel.Text = Server.Config.IRCOpChannels1;
            irc_chkPass.Checked = Server.Config.IRCIdentify1;
            irc_txtPass.Text = Server.Config.IRCPassword1;
            
            irc_cbTitles.Checked = Server.Config.IRCShowPlayerTitles1;
            irc_cbWorldChanges.Checked = Server.Config.IRCShowWorldChanges1;
            irc_cbAFK.Checked = Server.Config.IRCShowAFK1;
            ToggleIrcSettings1(Server.Config.UseIRC1);

            irc_cmbRank.Items.AddRange(GuiPerms.RankNames);
            GuiPerms.SetDefaultIndex(irc_cmbRank, Server.Config.IRCControllerRank1);
            irc_cmbVerify.Items.AddRange(Enum.GetNames(typeof(IRCControllerVerify1)));
            irc_cmbVerify.SelectedIndex = (int)Server.Config.IRCVerify;
            irc_txtPrefix.Text = Server.Config.IRCCommandPrefix;
        }
        
        void ApplyIRCProps1() {
            Server.Config.UseIRC1 = irc_chkEnabled.Checked;
            Server.Config.IRCServer1 = irc_txtServer.Text;
            Server.Config.IRCPort1 = (int)irc_numPort.Value;
            Server.Config.IRCNick1 = irc_txtNick.Text;
            Server.Config.IRCChannels1 = irc_txtChannel.Text;
            Server.Config.IRCOpChannels1 = irc_txtOpChannel.Text;
            Server.Config.IRCIdentify1 = irc_chkPass.Checked;
            Server.Config.IRCPassword1 = irc_txtPass.Text;
            
            Server.Config.IRCShowPlayerTitles1 = irc_cbTitles.Checked;
            Server.Config.IRCShowWorldChanges1 = irc_cbWorldChanges.Checked;
            Server.Config.IRCShowAFK1 = irc_cbAFK.Checked;
            
            Server.Config.IRCControllerRank1 = GuiPerms.GetPermission(irc_cmbRank, LevelPermission.Admin);
            Server.Config.IRCVerify1 = (IRCControllerVerify1)irc_cmbVerify.SelectedIndex;
            Server.Config.IRCCommandPrefix1 = irc_txtPrefix.Text;  
        }        
                
        void ToggleIrcSettings1(bool enabled) {
            irc_txtServer.Enabled = enabled; irc_lblServer.Enabled = enabled;
            irc_numPort.Enabled = enabled; irc_lblPort.Enabled = enabled;
            irc_txtNick.Enabled = enabled; irc_lblNick.Enabled = enabled;
            irc_txtChannel.Enabled = enabled; irc_lblChannel.Enabled = enabled;
            irc_txtOpChannel.Enabled = enabled; irc_lblOpChannel.Enabled = enabled;    
            irc_chkPass.Enabled = enabled; irc_txtPass.Enabled = enabled && irc_chkPass.Checked;
            
            irc_cbTitles.Enabled = enabled;
            irc_cbWorldChanges.Enabled = enabled;
            irc_cbAFK.Enabled = enabled;           
            irc_lblRank.Enabled = enabled; irc_cmbRank.Enabled = enabled;
            irc_lblVerify.Enabled = enabled; irc_cmbVerify.Enabled = enabled;
            irc_lblPrefix.Enabled = enabled; irc_txtPrefix.Enabled = enabled;
        }
        
        void irc_chkEnabled_CheckedChanged1(object sender, EventArgs e) {
            ToggleIrcSettings1(irc_chkEnabled.Checked);
        }        
        
        void irc_chkPass_CheckedChanged1(object sender, EventArgs e) {
            irc_txtPass.Enabled = irc_chkEnabled.Checked && irc_chkPass.Checked;
        }
        
        
        void LoadDiscordProps1() {
            Discord1Config cfg = DiscordPlugin1.Config;
            dis_chkEnabled.Checked = cfg.Enabled;
            dis_txtToken.Text      = cfg.BotToken;
            dis_txtChannel.Text    = cfg.Channels;
            dis_txtOpChannel.Text  = cfg.OpChannels;
            dis_chkNicks.Checked   = cfg.UseNicks;
            ToggleDiscordSettings1(cfg.Enabled);
        }
        
        void ApplyDiscordProps1() {
            Discord1Config cfg = DiscordPlugin1.Config;
            cfg.Enabled    = dis_chkEnabled.Checked;
            cfg.BotToken   = dis_txtToken.Text;
            cfg.Channels   = dis_txtChannel.Text;
            cfg.OpChannels = dis_txtOpChannel.Text;
            cfg.UseNicks   = dis_chkNicks.Checked;
        }
        
        void SaveDiscordProps1() {
            DiscordPlugin1.Config.Save();
        }
                
        void ToggleDiscordSettings1(bool enabled) {
            dis_txtToken.Enabled = enabled; dis_lblToken.Enabled = enabled;
            dis_txtChannel.Enabled = enabled; dis_lblChannel.Enabled = enabled;
            dis_txtOpChannel.Enabled = enabled; dis_lblOpChannel.Enabled = enabled;
            dis_chkNicks.Enabled = enabled;
        }
        
        void dis_chkEnabled_CheckedChanged1(object sender, EventArgs e) {
            ToggleDiscordSettings1(dis_chkEnabled.Checked);
        }

        void dis_lnkHelp_LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e) {
            GuiUtils.OpenBrowser(Updater.SourceURL + "/wiki/Discord-relay-bot/");
        }
    }
}
