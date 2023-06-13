/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with SuperNova)
 
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
namespace SuperNova.Commands.Info {
    public sealed class CmdHasirc1 : Command2 {
        public override string name { get { return "HasIRC1"; } }
        public override string shortcut { get { return "IRC1"; } }
        public override string type { get { return CommandTypes.Information; } }

        public override void Use(Player p, string message, CommandData data) {
            if (message.Length > 0) { Help(p); return; }

            if (Server.Config.UseIRC1) {
                p.Message("IRC1 is &aEnabled&S.");
                p.Message("Location: " + Server.Config.IRCServer1 + " > " + Server.Config.IRCChannels1);
            } else {
                p.Message("IRC1 is &cDisabled&S.");
            }
        }

        public override void Help(Player p) {
            p.Message("&T/HasIRC1");
            p.Message("&HOutputs whether the server has IRC1 enabled or not.");
            p.Message("&HIf IRC1 is enabled, server and channel are also displayed.");
        }
    }
}
