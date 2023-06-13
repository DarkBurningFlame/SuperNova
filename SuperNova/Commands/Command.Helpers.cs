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
using SuperNova.Commands;
using SuperNova.Eco;
using SuperNova.Games;

namespace SuperNova {
    
    public abstract partial class Command {

        const CommandEnable bothFlags = CommandEnable.Lava | CommandEnable.Zombie;
        public static string GetDisabledReason(CommandEnable enable) {
            if (enable == CommandEnable.Always) return null;
            if (enable == CommandEnable.Economy && !Economy.Enabled)
                return "economy is disabled.";
            
            if (enable == bothFlags && !(ZSGame.Instance.Running || LSGame.Instance.Running))
                return "neither zombie nor lava survival is running.";
            if (enable == CommandEnable.Zombie && !ZSGame.Instance.Running)
                return "zombie survival is not running.";
            if (enable == CommandEnable.Lava)
                return "lava survival is not running.";
            return null;
        }
        
        public bool CheckSuper(Player p, string message, string type) {
            if (message.Length > 0 || !p.IsSuper) return false;
            SuperRequiresArgs(name, p, type);
            return true;
        }
        
        public void SuperRequiresArgs(Player p, string type) { SuperRequiresArgs(name, p, type); }
        
        public static void SuperRequiresArgs(string cmd, Player p, string type) {
            p.Message("When using /{0} from {2}, you must provide a {1}.", cmd, type, p.SuperName);
        }
        
        public bool HasExtraPerm(Player p, string cmd, LevelPermission plRank, int num) {
            return CommandExtraPerms.Find(cmd, num).UsableBy(plRank);
        }
        
        public bool HasExtraPerm(Player p, LevelPermission plRank, int num) {
            return HasExtraPerm(p, name, plRank, num);
        }
        
        public bool CheckExtraPerm(Player p, CommandData data, int num) {
            if (HasExtraPerm(p, data.Rank, num)) return true;
            
            CommandExtraPerms perms = CommandExtraPerms.Find(name, num);
            perms.MessageCannotUse(p);
            return false;
        }
        
        public static bool CheckRank(Player p, CommandData data, Player target, 
                                                 string action, bool canAffectOwnRank) {
            return CheckRank(p, data, target.name, target.Rank, action, canAffectOwnRank);
        }
        
        public static bool CheckRank(Player p, CommandData data, 
                                                 string plName, LevelPermission plRank,
                                                 string action, bool canAffectOwnRank) {
            if (p.name.CaselessEq(plName)) return true;
#if DEV_BUILD_NOVA
            if (p.IsNova || plRank < data.Rank) return true;
#else
            if (p.IsConsole || plRank < data.Rank) return true;
#endif
            if (canAffectOwnRank && plRank == data.Rank) return true;
            
            if (canAffectOwnRank) {
                p.Message("Can only {0} players ranked {1} &Sor below", action, p.group.ColoredName);
            } else {
                p.Message("Can only {0} players ranked below {1}", action, p.group.ColoredName);
            }
            return false;
        }
        
        public string CheckOwn(Player p, string name, string type) {
            if (name.CaselessEq("-own")) {
                if (p.IsSuper) { SuperRequiresArgs(p, type); return null; }
                return p.name;
            }
            return name;
        }
        
        
        public static bool IsListModifier(string str) {
            int ignored;
            return str.CaselessEq("all") || int.TryParse(str, out ignored);
        }      
        
        public static bool IsCreateCommand(string str) {
            return str.CaselessEq("create") || str.CaselessEq("add") || str.CaselessEq("new");
        } 
        
        public static bool IsDeleteCommand(string str) {
            return str.CaselessEq("del") || str.CaselessEq("delete") || str.CaselessEq("remove");
        }
        
        public static bool IsEditCommand(string str) {
            return str.CaselessEq("edit") || str.CaselessEq("change") || str.CaselessEq("modify")
                || str.CaselessEq("move") || str.CaselessEq("update");
        }  

        public static bool IsInfoCommand(string str) {
            return str.CaselessEq("about") || str.CaselessEq("info") || str.CaselessEq("status")
                || str.CaselessEq("check");
        }
        
        public static bool IsListCommand(string str) {
            return str.CaselessEq("list") || str.CaselessEq("view");
        }
    }
    
    public sealed class CommandTypes {
        public const string Building = "build";
        public const string Chat = "chat";
        public const string Economy = "economy";
        public const string Games = "game";
        public const string Information = "information";
        public const string Moderation = "mod";
        public const string Other = "other";
        public const string World = "world";
    }
}
