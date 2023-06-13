/*
This file is licensed for use on the Evaporate I and Joka Craft servers.
Copyright catgirl 2023
It may not be used on other servers without catgirl's permission.
Anyone this file is licensed to may modify it however they want, but they cannot share it

Thanks
-catgrill

Seth if you're reading this, you can add this command to your servers too.
*/
//reference System.Net.Http.dll
//reference System.dll
//reference System.Core.dll
//reference Newtonsoft.Json.dll

using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MCGalaxy;

public class CmdCCApi : Command
{
	public override string name { get { return "CCApi"; } }

	public override string shortcut { get { return "ci"; } }

	// Which submenu this command displays in under /Help
	public override string type { get { return "other"; } }

	// Whether or not this command can be used in a museum. Block/map altering commands should return false to avoid errors.
	public override bool museumUsable { get { return true; } }

	// The default rank required to use this command. Valid values are:
	//   LevelPermission.Guest, LevelPermission.Builder, LevelPermission.AdvBuilder,
	//   LevelPermission.Operator, LevelPermission.Admin, LevelPermission.Owner
	public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

 private string Get(string path) {
   using(var client = new HttpClient()) {
        var response = client.GetAsync(path).GetAwaiter().GetResult();
        if (response.IsSuccessStatusCode) {
            var responseContent = response.Content;
            return responseContent.ReadAsStringAsync().GetAwaiter().GetResult();
        }
    }
    return "" ;
 }
 
 DateTime Timestamp( double unixTimeStamp )
{
    // Unix timestamp is seconds past epoch
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
    return dateTime;
} 

	public override void Use(Player p, string args) {
   if (p.IsSuper && args.Length <= 0) {
     SuperRequiresArgs(p, "player name"); return;
   }
   string name;
   if (args.SplitSpaces().Length == 0 && !p.IsSuper)
      name = p.truename;
   else
      name = args.SplitSpaces()[0];
   JObject o = JObject.Parse(Get("https://classicube.net/api/player/" + name));
   JObject c = JObject.Parse(Get("https://classicube.net/api/players/"));
   if (((string)o.SelectToken("error")) == "") {
       string username = (string)o.SelectToken("username");
       int id = (int)o.SelectToken("id");
       DateTime register = Timestamp((double)o.SelectToken("registered"));
       TimeSpan difference = DateTime.Today - register;
       bool premium = (bool)o.SelectToken("premium");
       string flags = "" ;
       foreach (JToken t in o.SelectToken("flags").Values()){
         flags += (char)t;
       }
       string title = (string)o.SelectToken("forum_title");
       //bool verified = (bool)o.SelectToken(" ");
       if (title != "")
          username = "(" + title + ") " + username;
       if (username.ToLower().EndsWith("s"))
           p.Message(username + "' ClassiCube profile:");
       else
           p.Message(username + "'s ClassiCube profile:");
       p.Message("ID: " + id.ToString() + " (out of " + ((int)c.SelectToken("playercount")).ToString() + ")");
       p.Message(
           "Registered at " 
           + register.ToString(
               "yyyy'-'MM'-'dd' 'HH':'mm':'ss"
           )
           + ", about "
           + difference.Days.ToString()
           + " days ago." 
       );
       if (flags != "")
          p.Message("Flags: " + flags);
       if (premium)
           p.Message("Flushes 125$ daily down the toilet.");
  }
  else {
    p.Message((string)o.SelectToken("error"));
    p.Message("(Was looking up \""+name+"\")");
}
	}

	// This is for when a player does /Help Ccapi
	public override void Help(Player p)
	{
		p.Message("/CCApi [player] - check a player's classicube profile");
	}
}