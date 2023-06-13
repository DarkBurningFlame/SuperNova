//Reference websocket-sharp.dll
//reference Newtonsoft.Json.dll
//reference System.dll
//reference System.Core.dll
//reference Microsoft.CSharp.dll
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using SuperNova;
using SuperNova.Events;
using SuperNova.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using WebSocketSharp;
using SuperNova.Config;
using SuperNova.DB;
using SuperNova.Events.PlayerEvents;


namespace SuperNova {
    public class Who
    {
        public struct GroupPlayers { public Group group; public StringBuilder builder; }
        public static GroupPlayers Make(Group group, bool showmaps, ref int totalPlayers)
        {
            GroupPlayers list;
            list.group = group;
            list.builder = new StringBuilder();

            Player[] online = PlayerInfo.Online.Items;
            foreach (Player pl in online)
            {
                if (pl.hidden) continue; // Never show hidden players
                if (pl.group != group) continue;
                totalPlayers++;
                Append(list, pl, showmaps);
            }
            return list;
        }

        static void Append(GroupPlayers list, Player p, bool showmaps)
        {
            StringBuilder data = list.builder;
            data.Append(' ');
            if (p.voice) { data.Append("+").Append(list.group.Color); }
			data.Append(Colors.Strip(Player.Nova.FormatNick(p)));


            if (p.muted) data.Append("-muted");
            if (p.frozen) data.Append("-frozen");
            if (p.Game.Referee) data.Append("-ref");
            if (p.IsAfk) data.Append("-afk");
            if (p.Unverified) data.Append("-unverified");

            if (!showmaps) { data.Append(","); return; }

            string lvlName = Colors.Strip(p.level.name); // for museums
            data.Append(" (").Append(lvlName).Append("),");
        }

        public static string GetPlural(string name)
        {
            if (name.Length < 2) return name;

            string last2 = name.Substring(name.Length - 2).ToLower();
            if ((last2 != "ed" || name.Length <= 3) && last2[1] != 's')
                return name + "s";
            return name;
        }

        public static string Output(GroupPlayers list)
        {
            StringBuilder data = list.builder;
            if (data.Length == 0) return null;
            if (data.Length > 0) data.Remove(data.Length - 1, 1);

            string title = "**" + GetPlural(list.group.Name) + "**:";
            return title + data + "\n";
        }
    }
}

public class REST : IDisposable
    {
        public const string UserAgent = "SuperNova-Discord (https://github.com/Fam0r/MCGalaxy-Discord)";
        public const string BaseURL = "https://discord.com/api/v8";
        CustomWebClient wc;

        public class CustomWebClient : WebClient
        {
            string token = "";
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest req = (HttpWebRequest)base.GetWebRequest(address);
                req.UserAgent = UserAgent;
                req.ContentType = "application/json; charset=utf-8";
                if (req.Method == "POST") req.Headers.Set("Authorization", "Bot " + token);
                return (WebRequest)req;
            }

            public CustomWebClient(string BotToken)
            {
                token = BotToken;
            }
        }

        public REST(string BotToken)
        {
            wc = new CustomWebClient(BotToken);
        }

        public void Dispose()
        {
            wc.Dispose();
        }

        public T GET<T>(string url)
        {
            return JsonConvert.DeserializeObject<T>(wc.DownloadString(url));
        }

        public int POST(string url, object data)
        {
            try
            {
                wc.UploadString(url, JsonConvert.SerializeObject(data));
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse res = (HttpWebResponse)ex.Response;
                    if (res == null) return 0;

                    return (int)res.StatusCode;
                }
                return 0;
            }
            return 200;
        }

        public T POST<T>(string url, object data)
        {
            return JsonConvert.DeserializeObject<T>(wc.UploadString(url, JsonConvert.SerializeObject(data)));
        }
    }

    public static class WS
    {
        public static Constants.WSPayload Deserialize(string data)
        {
            return JsonConvert.DeserializeObject<Constants.WSPayload>(data);
        }
    }

public class Discord2 : IDisposable {
		REST rest;
		WebSocket ws;

		public Constants.User user;
		public bool authenticated, beat;
		string gatewayURL, botToken, channelID, session_id;

		SchedulerTask heartbeatTask;
		int sequence;
		bool resetting;

		List<object> dataQueue = new List<object>();
		List<string> msgQueue = new List<string>();
		List<Constants.Embed> embedQueue = new List<Constants.Embed>();

		public Discord2(string token, string channelid) {
			botToken = token;
			channelID = channelid;

			Init();
		}

		public void Init() {
			rest = new REST(botToken);
			gatewayURL = GetGateway();
			ws = new WebSocket(gatewayURL + "?v=8&encoding=json");
			ws.OnMessage += OnMessage;
			ws.OnClose += OnClose;
			ws.Connect();
		}

		public void Dispose() {
			resetting = true;
			rest.Dispose();
			Server.Background.Cancel(heartbeatTask);
			if (ws != null) ws.Close(CloseStatusCode.Normal);
			((IDisposable)ws).Dispose();
		}

		public void Reset(CloseStatusCode statusCode = CloseStatusCode.Normal) {
			resetting = true;
			authenticated = false;

			rest.Dispose();
			Server.Background.Cancel(heartbeatTask);
			if (ws != null) ws.Close(statusCode);
			((IDisposable)ws).Dispose();
			Init();
		}

		string GetGateway() {
			Constants.Gateway data = rest.GET<Constants.Gateway>(REST.BaseURL + "/gateway");
			return data.url;
		}

		void OnClose(object sender, CloseEventArgs e) {
			Debug("Closed connection with code " + e.Code + " (" + e.Reason + ")");
			if (!resetting) Reset();
			SendMessage(channelID, "<@177424155371634688> closed with " + e.Code.ToString());
		}

		void Debug(string message) {
			SuperNova.Logger.Log(LogType.Debug, message);
		}



		void Beat(SchedulerTask task) {
			SendOP(Constants.OPCODE_HEARTBEAT);
		}

		public void SendStatusUpdate(string status, string activity, int type) {
			SendData(new Constants.StatusUpdate(status, activity, type));
		}

		void SendOP(int opcode) {
			object data = null;

			switch (opcode) {
				case Constants.OPCODE_HEARTBEAT:
					// Beat variable should pulse between true and false, true when heartbeat is sent and false when heartbeat is received
					// In the case the beat is true for 2 heartbeats in a row, consider the connection dead and create a new one
					if (beat) { Reset(CloseStatusCode.Abnormal); return; }
					beat = true;
					data = new Constants.HeartBeat(sequence);
					break;
				case Constants.OPCODE_IDENTIFY:
					data = new Constants.Identify(botToken);
					break;
				case Constants.OPCODE_RESUME:
					if (session_id == null || sequence == 0) return;
					data = new Constants.Resume(botToken, session_id, sequence);
					break;
			}

			if (data == null) return;
			SendData(data, true);
		}

		void SendData(object data, bool NoAuthCheck = false) {
			if (!authenticated && !NoAuthCheck) {
				Debug("Data queued for later. Not yet authenticated.");
				dataQueue.Add(data);
				return;
			}

			// Deal with data in queue first so things don't get sent out of order
			if (dataQueue.Count > 0) {
				object obj = dataQueue[0];
				dataQueue.RemoveAt(0);
				SendData(obj);
			}

			string j = JsonConvert.SerializeObject(data);
			ws.Send(j);
		}

		public void SendMessage(string ChannelID, string content) {
			if (!authenticated) {
				msgQueue.Add(content); return;
			}

			// Deal with data in queue first so things don't get sent out of order
			if (msgQueue.Count > 0) {
				string msg = msgQueue[0];
				msgQueue.RemoveAt(0);
				SendMessage(ChannelID, msg);
			}

			Constants.NewMessage newmsg = new Constants.NewMessage(content);
			int status = rest.POST(REST.BaseURL + "/channels/" + ChannelID + "/messages", newmsg);

			if (status == 429) {
				// Wait 2 seconds and retry when too many requests
				Thread.Sleep(2000);
				SendMessage(ChannelID, content);
			}
		}

		public void SendMessage(string ChannelID, Constants.Embed embed) {
			if (!authenticated) {
				embedQueue.Add(embed); return;
			}

			// Deal with data in queue first so things don't get sent out of order
			if (msgQueue.Count > 0) {
				Constants.Embed e = embedQueue[0];
				embedQueue.RemoveAt(0);
				SendMessage(ChannelID, e);
			}

			Constants.NewMessage newmsg = new Constants.NewMessage(embed, "");
			int status = rest.POST(REST.BaseURL + "/channels/" + ChannelID + "/messages", newmsg);

			if (status == 429) {
				// Wait 2 seconds and retry when too many requests
				Thread.Sleep(2000);
				SendMessage(ChannelID, embed);
			}
		}

		void Dispach(Constants.WSPayload payload) {
			switch (payload.t) {
				case "READY":
					Constants.Ready ready = new Constants.Ready(payload.d);
					user = ready.data.user;
					session_id = ready.data.session_id;
					SuperNova.Logger.Log(LogType.NovaMessage, "Logged in as " + user.username + "#" + user.discriminator);
					authenticated = true;
					break;

				case "MESSAGE_CREATE":
					Constants.Message msg = new Constants.Message(payload.d);
					if (msg.data.channel_id != channelID || msg.data.author.id == user.id) break;

					string nick = msg.data.author.username;
					if (msg.data.member.nick != null) nick = msg.data.member.nick;

					OnMessageReceivedEvent.Call(nick, msg.data.content);
					break;

			
				case "MESSAGE_UPDATE":
				case "CHANNEL_UPDATE":
					break;

				case "GUILD_CREATE":
					Constants.GuildCreate data = new Constants.GuildCreate(payload.d);
					foreach (Constants.Channel channel in data.data.channels) {
						if (channel.id == channelID) {
							Debug("Successfully authenticated!");
						}
					}
					break;

				case "RESUMED":
					authenticated = true;
					break;

				default:
					Debug("Unhandled dispach " + payload.t + ": " + payload.d);
					break;
			}
		}

		void OnMessage(object sender, MessageEventArgs e) {
			Constants.WSPayload payload = WS.Deserialize(e.Data);

			if (payload.s != null) sequence = payload.s.Value;

			switch (payload.op) {
				case Constants.OPCODE_DISPACH:
					Dispach(payload);
					break;

				case Constants.OPCODE_HELLO:
					TimeSpan delay = TimeSpan.FromMilliseconds(payload.d.Value<int>("heartbeat_interval"));
					if (heartbeatTask == null) heartbeatTask = Server.Background.QueueRepeat(Beat, null, delay);
					else heartbeatTask.Delay = delay;

					if (resetting) SendOP(Constants.OPCODE_RESUME);
					else SendOP(Constants.OPCODE_IDENTIFY);
					resetting = false;
					break;

				case Constants.OPCODE_ACK:
					beat = false;
					break;

				case Constants.OPCODE_INVALID_SESSION:
					Thread.Sleep(4000); // supposed to be a random number between 1 and 5 seconds. I swear I rolled the dice
					SendOP(Constants.OPCODE_IDENTIFY);
					break;

				case Constants.OPCODE_RECONNECT:
					Debug("Discord asked to reconnect");
					Reset();
					break;

				default:
					Debug("Unhandled opcode " + payload.op.ToString() + ": " + payload.d);
					break;
			}
		}
	}

	public delegate void OnMessageReceived(string nick, string message);
	public sealed class OnMessageReceivedEvent : IEvent<OnMessageReceived> {
		public static void Call(string nick, string message) {
			if (handlers.Count == 0) return;
			CallCommon(pl => pl(nick, message));
		}
	}

	public abstract class Constants
{

    #region opcodes
    public const int OPCODE_DISPACH = 0;
    public const int OPCODE_HEARTBEAT = 1;
    public const int OPCODE_IDENTIFY = 2;
    public const int OPCODE_PRESENCE_UPDATE = 3;
    public const int OPCODE_VOICE_STATE_UPDATE = 4;
    public const int OPCODE_RESUME = 6;
    public const int OPCODE_RECONNECT = 7;
    public const int OPCODE_REQUEST_GUILD_MEMBERS = 8;
    public const int OPCODE_INVALID_SESSION = 9;
    public const int OPCODE_HELLO = 10;
    public const int OPCODE_ACK = 11;
    #endregion

    #region intents
    const int INTENT_GUILDS = 1 << 0;
    const int INTENT_GUILD_MEMBERS = 1 << 1; // Privileged
    const int INTENT_GUILD_BANS = 1 << 2;
    const int INTENT_GUILD_EMOJIS = 1 << 3;
    const int INTENT_GUILD_INTEGRATIONS = 1 << 4;
    const int INTENT_GUILD_WEBHOOKS = 1 << 5;
    const int INTENT_GUILD_INVITES = 1 << 6;
    const int INTENT_GUILD_VOICE_STATES = 1 << 7;
    const int INTENT_GUILD_PRESENCES = 1 << 8; // Privileged
    const int INTENT_GUILD_MESSAGES = 1 << 9;
    const int INTENT_GUILD_MESSAGE_REACTIONS = 1 << 10;
    const int INTENT_GUILD_MESSAGE_TYPING = 1 << 11;
    const int INTENT_DIRECT_MESSAGES = 1 << 12;
    const int INTENT_DIRECT_MESSAGE_REACTIONS = 1 << 13;
    const int INTENT_DIRECT_MESSAGE_TYPING = 1 << 14;
    #endregion

    #region Common
    public class User
    {
        public string id { get; set; }
        public string username { get; set; }
        public string discriminator { get; set; }
        public string avatar { get; set; }
    }

    public class Member
    {
        public string nick { get; set; }
    }

    public class Channel
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Message
    {
        public class Data
        {
            public string id { get; set; }
            public string channel_id { get; set; }
            public string guild_id { get; set; }
            public User author { get; set; }
            public Member member { get; set; }
            public string content { get; set; }
            public bool? tts { get; set; }
            public Embed[] embeds { get; set; }
        }

        public Data data { get; set; }
        public Message(JObject d)
        {
            data = d.ToObject<Data>();
        }
    }

    public class Embed
    {
        //public DateTime timestamp { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public int color { get; set; }
        public EmbedFooter footer { get; set; }
        public EmbedAuthor author { get; set; }
        public EmbedField[] fields { get; set; }
    }
    public class EmbedFooter
    {
        public string text { get; set; }
        public string icon_url { get; set; }
    }
    public class EmbedAuthor
    {
        public string name { get; set; }
        public string url { get; set; }
        public string icon_url { get; set; }
    }
    public class EmbedField
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool inline { get; set; }
    }

    //public class Guild {}
    //public class Application {}
    #endregion

    #region REST
    public class Gateway
    {
        public string url { get; set; }
    }

    public class NewMessage
    {
        public string content { get; set; }
        public Embed embed { get; set; }
        public AllowedMentions allowed_mentions { get; set; }

        public class AllowedMentions
        {
            public string[] parse { get; set; }

            public AllowedMentions()
            {
                parse = new string[0]; // no pings allowed for now
            }
        }

        public NewMessage(string msgcontent)
        {
            allowed_mentions = new AllowedMentions();
            content = msgcontent;
        }
        public NewMessage(Embed embeds, string msgcontent)
        {
            allowed_mentions = new AllowedMentions();
            content = msgcontent;
            embed = embeds;
        }
    }
    #endregion


    #region WebSocket Payloads
    public class WSPayload
    {
        public int op { get; set; } // opcode
        public int? s { get; set; } // sequence
        public string t { get; set; } // type
        public dynamic d { get; set; } // data: JObject, int or null
    }

    public class HeartBeat : WSPayload
    {
        public HeartBeat(int sequence)
        {
            op = OPCODE_HEARTBEAT;
            d = sequence;
        }
    }

    public class Ready : WSPayload
    {
        public class Data
        {
            public int v { get; set; }
            public User user { get; set; }
            public Channel[] private_channels { get; set; }
            //public Guild[] guilds { get; set; }
            public string session_id { get; set; }
            // public Application application { get; set; }
        }
        public Data data { get; set; }
        public Ready(JObject d)
        {
            data = d.ToObject<Data>();
        }
    }

    public class Resume : WSPayload
    {
        public class Data
        {
            public string token { get; set; }
            public string session_id { get; set; }
            public int seq { get; set; }
        }
        public Data data { get; set; }
        public Resume(string token, string session_id, int seq)
        {
            op = OPCODE_RESUME;
            Data opts = new Data();

            opts.token = token;
            opts.session_id = session_id;
            opts.seq = seq;

            d = JObject.FromObject(opts);
        }
    }

    public class GuildCreate : WSPayload
    {
        public class Data
        {
            public string id { get; set; }
            public string name { get; set; }
            public Channel[] channels { get; set; }
        }
        public Data data { get; set; }
        public GuildCreate(JObject d)
        {
            data = d.ToObject<Data>();
        }
    }

    public class StatusUpdate : WSPayload
    {
        public class Data
        {
            public int? since { get; set; }
            public Activity[] activities { get; set; }
            public string status { get; set; }
            public bool afk { get; set; }
        }
        public StatusUpdate(string status, string activity, int type)
        {
            op = OPCODE_PRESENCE_UPDATE;

            Data opts = new Data();
            opts.since = null;
            Activity[] activities = new Activity[1];
            activities[0] = new Activity
            {
                name = activity,
                type = type // watching
            };
            opts.activities = activities;
            opts.status = status;
            opts.afk = false;

            d = JObject.FromObject(opts);
        }
    }

    public class Identify : WSPayload
    {
        class Data
        {
            public string token { get; set; }
            public properties properties { get; set; }
            public int intents { get; set; }
            public bool guild_subscriptions { get; set; }
            // presence

            public Data()
            {
                properties = new properties();
            }
        }
        class properties
        {
            public string os { get; set; }
            public string browser { get; set; }
            public string device { get; set; }
        }

        public Identify(string token)
        {
            op = OPCODE_IDENTIFY;

            Data opts = new Data();
            opts.token = token;
            opts.intents = INTENT_GUILDS | INTENT_GUILD_MESSAGES;
            opts.guild_subscriptions = false;

            opts.properties.os = opts.properties.browser = opts.properties.device = "MCGalaxy-Discord";

            d = JObject.FromObject(opts);
        }
    }
    #endregion

    #region Gateway objects
    public class Activity
    {
        public string name { get; set; }
        public int type { get; set; }
    }
    #endregion
}

public class PluginDiscord : Plugin
{
    public override string name { get { return "DiscordPlugin"; } }
    public override string SuperNova_Version { get { return "1.9.1.4"; } }
    public override string creator { get { return ""; } }

    public const string ConfigFile = "properties/discord3.properties";
    public static DiscordConfig config = new DiscordConfig();

    public static Discord2 dc;
    bool registered;

    public override void Load(bool startup)
    {
        config.LoadConfig();
        if (config.Token == "" || config.ChannelID == "")
        {
            SuperNova.Logger.Log(LogType.Warning, "Invalid config! Please setup the Discord3 bot in discord3.properties! (plugin reload required)");
            return;
        }

        dc = new Discord2(config.Token, config.ChannelID);

        OnPlayerConnectEvent.Register(PlayerConnect, Priority.Low);
        OnPlayerDisconnectEvent.Register(PlayerDisconnect, Priority.Low);
        OnPlayerChatEvent.Register(PlayerChat, Priority.Low);
        OnPlayerCommandEvent.Register(PlayerCommand, Priority.Low);
        OnModActionEvent.Register(ModAction, Priority.Low);

        OnMessageReceivedEvent.Register(DiscordMessage, Priority.Low);

        Command.Register(new CmdDiscordBot3());
        registered = true;
    }

    public override void Unload(bool shutdown)
    {
        if (dc != null) dc.Dispose();
        if (!registered) return;
        OnPlayerConnectEvent.Unregister(PlayerConnect);
        OnPlayerDisconnectEvent.Unregister(PlayerDisconnect);
        OnPlayerChatEvent.Unregister(PlayerChat);
        OnPlayerCommandEvent.Unregister(PlayerCommand);

        OnMessageReceivedEvent.Unregister(DiscordMessage);

        Command.Unregister(Command.Find("DiscordBot3"));
    }



    void PlayerCommand(Player p, string cmd, string args, CommandData data)
    {
        if (cmd != "hide") return;

        // Offset the player count by one if player is going to hide
        // Has to be done because this event is called before /hide is called
        if (p.hidden)
        {
            SetPresence(1);
            string message = config.DiscordPrefix + config.ConnectPrefix + " " + p.DisplayName + " " + PlayerDB.GetLoginMessage(p);
            SendMessage(Colors.Strip(message));
        }
        else
        {
            SetPresence(-1);
            string message = config.DiscordPrefix + config.DisconnectPrefix + " " + p.DisplayName + " " + PlayerDB.GetLogoutMessage(p);
            SendMessage(Colors.Strip(message));
        }
    }


    void ModAction(ModAction e)
    {
        if (!e.Announce) return;
        string message = config.DisconnectPrefix + e.FormatMessage(e.Target, GetActionType(e.Type));
        SendMessage(Colors.Strip(message));
    }

    void PlayerChat(Player p, string message)
    {
        if (p.cancelchat || !p.level.Config.ServerWideChat) return;
        message = config.DiscordPrefix + config.DiscordMessage.Replace("{name}", p.DisplayName).Replace("{msg}", message);
        SendMessage(Colors.Strip(message));
    }

    void PlayerDisconnect(Player p, string reason)
    {
        SetPresence();

        if (p.hidden) return;
        if (reason == null) reason = PlayerDB.GetLogoutMessage(p);
        string message = config.DiscordPrefix + config.DisconnectPrefix + " " + p.DisplayName + " " + reason;
        SendMessage(Colors.Strip(message));
    }

    void PlayerConnect(Player p)
    {
        SetPresence();

        if (p.hidden) return;
        string message = config.DiscordPrefix + config.ConnectPrefix + " " + p.DisplayName + " " + PlayerDB.GetLoginMessage(p);
        SendMessage(Colors.Strip(message));
    }

    void DiscordMessage(string nick, string message)
    {
        if (message.CaselessEq(".who") || message.CaselessEq(".players") || message.CaselessEq("!players"))
        {
            Constants.Embed embed = new Constants.Embed();
            embed.color = config.EmbedColor;
            embed.title = Server.Config.Name;

            Dictionary<string, List<string>> ranks = new Dictionary<string, List<string>>();

            int totalPlayers = 0;
            List<Who.GroupPlayers> allPlayers = new List<Who.GroupPlayers>();

            if (totalPlayers == 1) embed.description = "**There is 1 player online**\n\n";
            else embed.description = "**There are " + PlayerInfo.Online.Count + " players online**\n\n";

            if (config.zsmode && SuperNova.Games.ZSGame.Instance.Running)
            {
                foreach (Group grp in Group.GroupList)
                {
                    allPlayers.Add(Who.Make(grp, false, ref totalPlayers));
                }

                for (int i = allPlayers.Count - 1; i >= 0; i--)
                {
                    embed.description += Who.Output(allPlayers[i]);
                }

                embed.description += "\n" + "Map: `" + SuperNova.Games.ZSGame.Instance.Map.name + "`";
            }
            else
            {
                foreach (Group grp in Group.GroupList)
                {
                    allPlayers.Add(Who.Make(grp, true, ref totalPlayers));
                }

                for (int i = allPlayers.Count - 1; i >= 0; i--)
                {
                    embed.description += Who.Output(allPlayers[i]);
                }
            }

            SendMessage(embed);
            return;
        }

        message = config.IngameMessage.Replace("{name}", nick).Replace("{msg}", message);
        Chat.Message(ChatScope.Global, message, null, (Player pl, object arg) => !pl.Ignores.IRC);
    }


    static void SetPresence(int offset = 0)
    {
        if (!config.UseStatus) return;
        int count = PlayerInfo.NonHiddenCount();
        if (offset != 0) count += offset;

        string s = count == 1 ? "" : "s";
        string message = config.ActivityName.Replace("{p}", count.ToString()).Replace("{s}", s);

        dc.SendStatusUpdate(config.Status.ToString(), message, (int)config.Activity);
    }

    public static void SendMessage(Constants.Embed message)
    {
        // Queue a message so the message doesn't have to wait until discord receives it to display in chat
        Server.Background.QueueOnce(SendMessage, message, TimeSpan.Zero);
    }
    public static void SendMessage(string message)
    {
        Server.Background.QueueOnce(SendMessage, message, TimeSpan.Zero);
    }

    static void SendMessage(SchedulerTask task)
    {
        if (task.State is Constants.Embed)
        {
            dc.SendMessage(config.ChannelID, (Constants.Embed)task.State);
        }
        else if (task.State is string)
        {
            dc.SendMessage(config.ChannelID, (string)task.State);
        }
    }

    public static void ReloadConfig()
    {
        config.LoadConfig();
    }

    string GetActionType(ModActionType type)
    {
        switch (type)
        {
            case ModActionType.Ban:
                return "banned";
            case ModActionType.BanIP:
                return "IP Banned";
            case ModActionType.UnbanIP:
                return "IP Unbanned";
            case ModActionType.Muted:
                return "Muted";
            case ModActionType.Unmuted:
                return "Unmuted";
            case ModActionType.Frozen:
                return "Frozen";
            case ModActionType.Unfrozen:
                return "Unfrozen";
            case ModActionType.Warned:
                return "Warned";
            case ModActionType.Rank:
                return "Ranked";
            case ModActionType.Kicked:
                return "Kicked";
            default:
                return "Punished";
        }
    }

    public class DiscordConfig
    {
        [ConfigString("token", "Account", "", true)]
        public string Token = "";

        [ConfigString("channel-id", "Account", "", true)]
        public string ChannelID = "";

        [ConfigEnum("status", "Status", ClientStatus.online, typeof(ClientStatus))]
        public ClientStatus Status = ClientStatus.online;

        [ConfigEnum("activity", "Status", Activities.playing, typeof(Activities))]
        public Activities Activity = Activities.playing;

        [ConfigString("activity-name", "Status", "with {p} players", false)]
        public string ActivityName = "with {p} players";

        [ConfigBool("use-status", "Status", true)]
        public bool UseStatus = true;

        [ConfigString("discord-prefix", "Formatting", "", true)]
        public string DiscordPrefix = "";

        [ConfigString("discord-message", "Formatting", "{name}: {msg}", true)]
        public string DiscordMessage = "{name}: {msg}";

        [ConfigString("ingame-message", "Formatting", "(Discord) &f{name}: {msg}}", true)]
        public string IngameMessage = Server.Config.IRCColor + "(Discord) &f{name}: {msg}";

        [ConfigString("connect-prefix", "Formatting", "+", false)]
        public string ConnectPrefix = "+";

        [ConfigString("disconnect-prefix", "Formatting", "-", false)]
        public string DisconnectPrefix = "-";

        [ConfigInt("embed-color", "Formatting", 0xaafaaa)]
        public int EmbedColor = 0xaafaaa;

        [ConfigBool("zsmode", "Formatting", false)]
        public bool zsmode = false;

        public enum ClientStatus
        {
            online,
            dnd,
            idle,
            invisible
        }

        public enum Activities
        {
            playing,
            streaming, // unused
            listening,
            watching, // undocumented
            custom, // unused
            competing
        }

        internal static ConfigElement[] cfg;
        public void LoadConfig()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(DiscordConfig));
            PropertiesFile.Read(ConfigFile, LineProcessor);
            SaveConfig();

            if (config.DiscordPrefix != "") config.DiscordPrefix += " "; // add space after prefix, trim removes it
        }

        void LineProcessor(string key, string value)
        {
            ConfigElement.Parse(cfg, config, key, value);
        }

        readonly object saveLock = new object();
        public void SaveConfig()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(DiscordConfig));
            try
            {
                lock (saveLock)
                {
                    using (StreamWriter w = new StreamWriter(ConfigFile))
                        SaveProps(w);
                }
            }
            catch (Exception ex)
            {
                SuperNova.Logger.LogError("Error saving " + ConfigFile, ex);
            }
        }

        void SaveProps(StreamWriter w)
        {
            w.WriteLine("# To get the token, go to https://discord.com/developers/applications and create a new application.");
            w.WriteLine("# Select the app and go to the Bot tab. Add bot and copy the token below");
            w.WriteLine("# Make sure the bot is invited to the server that contains the channel ID provided");
            w.WriteLine("# Invite URL can be generated by going to the OAuth2 tab and ticking bot in the scopes");
            w.WriteLine("#");
            w.WriteLine("# Account settings require restarting. Other settings can be reloaded with /DiscordBot reload");
            w.WriteLine("#");
            w.WriteLine("# Possible status values: online, dnd, idle, invisible");
            w.WriteLine("# Possible activity values: playing, listening, watching, competing");
            w.WriteLine("# {p} is replaced with the player count in activity-name. {s} is 's' when there are multiple players online, and empty when there's one");
            w.WriteLine("#");
            w.WriteLine("# discord-prefix adds a prefix to messages from Discord to CC (including connect/disconnect)");
            w.WriteLine("# discord-message is message sent from CC to Discord");
            w.WriteLine("# ingame-message is message sent from Discord to CC");
            w.WriteLine("# {name} is replaced with the player name");
            w.WriteLine("# {msg} is replaced with the message");
            w.WriteLine("#");
            w.WriteLine("# Connect formatting is:");
            w.WriteLine("# [message-prefix][connect-prefix] <name> <joinmessage>");
            w.WriteLine("# Disconnect formatting is the same");
            w.WriteLine("#");
            w.WriteLine("# embed-color is the color used in embeds, as an integer");
            w.WriteLine();

            ConfigElement.Serialise(cfg, w, config);
        }
    }
}

public sealed class CmdDiscordBot3 : Command2
{
    public override string name { get { return "DiscordBot3"; } }
    public override string type { get { return CommandTypes.Other; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

    public override void Use(Player p, string message, CommandData data)
    {
        if (message == "") { Help(p); return; }
        string[] args = message.SplitSpaces(2);

        switch (args[0])
        {
            case "reload": ReloadConfig(p); return;
            case "restart": RestartBot(p); return;
        }
    }

    void ReloadConfig(Player p)
    {
        PluginDiscord.ReloadConfig();
        p.Message("Discord3 config reloaded.");
    }

    void RestartBot(Player p)
    {
        PluginDiscord.dc.Dispose();
        PluginDiscord.dc = new Discord2(PluginDiscord.config.Token, PluginDiscord.config.ChannelID);
        p.Message("Discord3 bot restarted.");
    }

    public override void Help(Player p)
    {
        p.Message("%T/DiscordBot3 reload - %HReload config files");
        p.Message("%T/DiscordBot3 restart - %HRestart the bot");
        p.Message("%HToken or Channel ID changes require a restart after reloading the config");
    }
}
