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
    public sealed class CmdHasirc2 : Command2 {
        public override string name { get { return "HasIRC2"; } }
        public override string shortcut { get { return "IRC2"; } }
        public override string type { get { return CommandTypes.Information; } }

        public override void Use(Player p, string message, CommandData data) {
            if (message.Length > 0) { Help(p); return; }

            if (Server.Config.UseIRC2) {
                p.Message("IRC2 is &aEnabled&S.");
                p.Message("Location: " + Server.Config.IRCServer2 + " > " + Server.Config.IRCChannels2);
            } else {
                p.Message("IRC2 is &cDisabled&S.");
            }
        }

        public override void Help(Player p) {
            p.Message("&T/HasIRC2");
            p.Message("&HOutputs whether the server has IRC2 enabled or not.");
            p.Message("&HIf IRC2 is enabled, server and channel are also displayed.");
        }
    }
}
