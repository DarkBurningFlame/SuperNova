/*
    Copyright 2011 MCForge
        
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
namespace SuperNova.Commands.Misc {
    public sealed class CmdInvulnerable : Command2 {
        public override string name { get { return "Invulnerable"; } }
        public override string shortcut { get { return "Inv"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandAlias[] Aliases {
            get { return new CommandAlias[] { new CommandAlias("GodMode") }; }
        }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can toggle invulnerability of others") }; }
        }
        
        public override void Use(Player p, string message, CommandData data) {
            Player who = message.Length == 0 ? p : PlayerInfo.FindMatches(p, message);
            if (who == null) return;

            if (p != who && !CheckExtraPerm(p, data, 1)) return;
            if (!CheckRank(p, data, who, "toggle invulnerability", true)) return;
            
            who.invulnerable = !who.invulnerable;
            ShowPlayerMessage(p, who);
        }
        
        static void ShowPlayerMessage(Player p, Player target) {
            string msg = target.invulnerable ? "now invulnerable" : "no longer invulnerable";
            if (p == target) p.Message("You are {0}", msg);

            string globalMsg = target.invulnerable ? Server.Config.InvulnerableMessage : "has stopped being invulnerable";
            if (Server.Config.ShowInvulnerableMessage && !target.hidden) {
                Chat.MessageFrom(target, "λNICK &S" + globalMsg);
            } else if (p != target) {
                p.Message("{0} &Sis {1}.", p.FormatNick(target), msg);
            }
        }
        
        public override void Help(Player p) {
            p.Message("&T/Invulnerable <name>");
            p.Message("&HTurns invulnerable mode on/off.");
            p.Message("&HIf <name> is given, that player's invulnerability is toggled");
        }
    }
}
