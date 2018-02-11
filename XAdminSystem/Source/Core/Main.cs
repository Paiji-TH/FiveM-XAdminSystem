using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using MySql.Data.MySqlClient;
using XAdminSystem.Core.Data;
using System.Diagnostics;
using XAdminSystem.Core.Commands;
using System.Reflection;
using XAdminSystem.Core.Database;

namespace XAdminSystem
{
    public class Main : BaseScript
    {
        public static Version version;
        public static PlayerData lastJoinedPlayer;
        public int lastJoinReconnectCounter = 0;
        public Stopwatch lastJoinedStopWatch;
        public static List<PlayerData> players = new List<PlayerData>();
        public static List<ChatCommand> commands = new List<ChatCommand>();
        public static List<Permission> permissions = new List<Permission>();
        public static List<Group> groups = new List<Group>();
        public static List<Role> roles = new List<Role>();

        public static bool whitelist_enabled = false;

        public Main()
        {
            version = Assembly.GetExecutingAssembly().GetName().Version;

            // Sub-Memory Scope.
            {
                string ipaddress = API.GetConvar("SQL_IPADDRESS", "127.0.0.1");
                int portAddress = API.GetConvarInt("SQL_PORTADDRESS", 3306);
                string username = API.GetConvar("SQL_USERNAME", "root");
                string password = API.GetConvar("SQL_PASSWORD", "");
                string database = API.GetConvar("SQL_DATABASE", "XAdminSystem");

                Task task = MySQL.ConnectAsync(ipaddress, portAddress, username, password, database);
                task.Start();
            }
            
            // Default Groups
            groups.Add(new Group("user", false));

            // Default Commands
            commands.Add(new Help());

            // Default Permissions
            permissions.Add(new Permission("asay"));

            // Main Event Listeners
            EventHandlers["playerConnecting"] += new Action<Player, string, CallbackDelegate>(PlayerConnected);
            EventHandlers["playerDropped"] += new Action<Player>(PlayerDisconnected);
            EventHandlers["chatMessage"] += new Action<Player, string, string>(OnPlayerText);

            // Player Connected/Disconnected
            EventHandlers.Add("xa:PlayerConnected", new Action<PlayerData>(XA_PlayerConnected));
            EventHandlers.Add("xa:PlayerDisconnected", new Action<PlayerData>(XA_PlayerDisconnected));

            EventHandlers.Add("xa:ChatInvalidCommand", new Action<PlayerData, string[]>(ChatInvalidCommand));

            // Chat Management
            EventHandlers.Add("xa:AddChatCommand", new Action<ChatCommand, CallbackDelegate>(AddChatCommand));
            EventHandlers.Add("xa:AddChatGroupCommand", new Action<ChatCommand, CallbackDelegate>(AddChatGroupCommand));

            // Group Management
            EventHandlers.Add("xa:AddUserGroup", new Action<Group>(AddUserGroup));

            // Adding to Groups
            EventHandlers.Add("xa:AddUserToGroup", new Action<PlayerData, Group>(AddUserToGroup));
        }

        #region Chat Messages
        public static void AdminChatMessage(string message)
        {
            foreach (PlayerData player in players)
            {
                if (player.hasPermission("asay"))
                {
                    player.SendMessage(message);
                }
            }
        }

        public static void BroadcastChatMessage(string message, string title = "")
        {
            foreach(PlayerData player in players)
            {
                player.SendMessage(message, title);
            }
        }

        public static void ChatMessage(PlayerData player, string message, string title = "")
        {
            player.SendMessage(message, title);
        }
        #endregion 

        #region Custom EventHandlers
        private void AddUserToGroup(PlayerData player, Group group)
        {
            player.setUserGroup(group);
            Console.WriteLine("\n "+ player.getUserName() +" was set to "+ group.getName() +" \n");
        }

        private void AddUserGroup(Group obj)
        {
            Group group = null;
            foreach(Group g in groups)
            {
                if (g.getName() == group.getName())
                {
                    group = g;
                    break;
                }
            }

            if (group != null) return;

            groups.Add(group);
        }

        private void AddChatGroupCommand(ChatCommand command, CallbackDelegate cb)
        {
            ChatCommand chat = null;
            foreach(ChatCommand cmd in commands)
            {
                if (cmd.getName() == command.getName())
                {
                    chat = cmd;
                    break;
                }
            }

            if (chat != null) return;

            commands.Add(chat);
        }

        private void AddChatCommand(ChatCommand command, CallbackDelegate cb)
        {
            ChatCommand chat = null;

            foreach (ChatCommand cmd in commands)
            {
                if (cmd.getName() == command.getName())
                {
                    chat = cmd;
                    break;
                }
            }

            if (chat != null) return;

            commands.Add(chat);
        }

        private void ChatInvalidCommand(PlayerData arg1, string[] arg2)
        {
        }

        private void XA_PlayerDisconnected(PlayerData obj)
        {
            Console.WriteLine("Player Disconnected -> " + obj.getUserName() + "[" + obj.getHandler().EndPoint.ToString() + " | " + obj.getIdentifier() + "]");
        }

        private void XA_PlayerConnected(PlayerData obj)
        {
            Console.WriteLine("Player Connected -> " + obj.getUserName() + "["+ obj.getHandler().EndPoint.ToString() +" | "+ obj.getIdentifier() +"]");
        }
        #endregion

        #region Base EventHandlers
        /// <summary>
        /// This will be called when a player connects.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playername"></param>
        /// <param name="kickCallback"></param>
        private void PlayerConnected([FromSource]Player player, string playername, CallbackDelegate kickCallback)
        {
            // Make sure that the Client has Steam enabled.
            if (player.Identifiers["steam"] == null)
            {
                player.Drop("STEAM CLIENT not detected, please start or restart your Steam Client to come back.");
                API.CancelEvent();
                return;
            }

            // Make sure the client isn't banned.
            AdminDB.isBanned(player, new Action<PlayerData, bool>((p, isBanned) => {
                if(isBanned)
                {
                    AdminDB.getBannedMessage(player, true, true, new Action<PlayerData, string>((ply, message) =>
                    {
                        ply.getHandler().Drop(message);
                        API.CancelEvent();
                    }));

                    return;
                }
            }));

            if(whitelist_enabled)
            {
                AdminDB.isWhitelisted(player, new Action<PlayerData, bool>((ply, isWhitelisted) =>
                {
                    if (!isWhitelisted)
                    {
                        API.CancelEvent();
                        return;
                    }
                }));
            }

            // Search the player.
            PlayerData joiningPlayer = null;
            foreach(PlayerData ply in players)
            {
                if (ply.getIdentifier() == player.Identifiers["steam"])
                {
                    joiningPlayer = ply;
                    break;
                }
            }

            // Kicks player for spam reconnecting.
            if(lastJoinedPlayer != null)
            {
                if (lastJoinedPlayer.getIdentifier() == player.Identifiers["steam"])
                {
                    long seconds = lastJoinedStopWatch.ElapsedMilliseconds * 1000;
                    if (lastJoinReconnectCounter >= 5 && seconds < 5)
                    {
                        lastJoinedPlayer.BanAsync(null, "Reconnecting to quickly. Come back later.", "1h");
                        lastJoinReconnectCounter = 0;
                    }
                    else
                    {
                        lastJoinReconnectCounter++;
                    }
                }
                else
                {
                    lastJoinReconnectCounter = 0;
                }
            }

            lastJoinedPlayer = new PlayerData(player);

            // Clear Memory, if necessary.
            if (lastJoinedStopWatch != null)
                lastJoinedStopWatch.Reset();

            lastJoinedStopWatch = new Stopwatch();
            lastJoinedStopWatch.Start();

            if (joiningPlayer == null)
            {
                PlayerData ply = new PlayerData(player);
                players.Add(ply);
                TriggerEvent("ax:PlayerConnected", ply);
            }
        }

        /// <summary>
        /// This will be ran when a player disconnected.
        /// </summary>
        /// <param name="player"></param>
        private void PlayerDisconnected([FromSource]Player player)
        {

            // Remove PlayerData from the list, so it clears memory for the server.
            foreach(PlayerData ply in players)
            {
                if(ply.getIdentifier() == player.Identifiers["steam"])
                {
                    TriggerEvent("ax:PlayerDisconnected", ply);
                    players.Remove(ply);
                    break;
                }
            }
        }

        /// <summary>
        /// This will be ran when a player types a message.
        /// This is to detect commands, and process them.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="userName"></param>
        /// <param name="message"></param>
        private void OnPlayerText([FromSource]Player pl, string userName, string message)
        {
            List<string> args = message.Split(' ').ToList();

            ChatCommand found = null;

            PlayerData player = null;

            foreach(PlayerData ply in players)
            {
                if (ply.getIdentifier() == pl.Identifiers["steam"])
                {
                    player = ply;
                    break;
                }
            }

            // Stop if the player wasn't found.
            if (player == null) return;

            // Stop if the player didn't actually do a command.
            if (!args[0].StartsWith("/")) { TriggerEvent("ax:ChatMessage", args); return; }

            foreach(ChatCommand cmd in commands)
            {
                if (args[0] == "/" + cmd.getCommandText())
                {
                    args.RemoveAt(0);
                    if(player.hasPermission(cmd))
                    {
                        cmd.DoCommand(args.ToArray<string>());
                    }
                    found = cmd;
                }
            }

            if (found != null)
                TriggerEvent("ax:ChatInvalidCommand", found.getCommandText());
        }
        #endregion
    }
}
