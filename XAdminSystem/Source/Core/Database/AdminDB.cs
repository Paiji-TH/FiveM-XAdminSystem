using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAdminSystem.Core.Data;

namespace XAdminSystem.Core.Database
{
    public class AdminDB
    {
        public async static void setupDatabase()
        {
            await MySQL.ExecuteSQLAsync(@"
                CREATE TABLE IF NOT EXIST players 
                (
                    ID INT(11) NOT NULL AUTO_INCREMENT,
                    USERNAME TEXT NOT NULL,
                    OTHER_USERNAMES TEXT NOT NULL,
                    IP_ADDRESSES TEXT NOT NULL,
                    STEAMID_ADDRESS TEXT NOT NULL,
                    GROUP TEXT NOT NULL,
                    ROLES TEXT NOT NULL,
                    PRIMARY KEY(ID)
                );

                CREATE TABLE IF NOT EXIST bans
                (
                    ID INT(11) NOT NULL AUTO_INCREMENT,
                    BANNED_PID INT(11) NOT NULL,
                    BANNER_PID INT(11) NOT NULL,
                    REASON TEXT NOT NULL,
                    RELEASE_DATE TEXT NOT NULL,
                    CREATION_DATE TEXT NOT NULL,
                    PRIMARY KEY(ID)
                );

                CREATE TABLE IF NOT EXIST whitelists
                (
                    ID INT(11) NOT NULL AUTO_INCREMENT,
                    WHITELIST_PID INT(11) NOT NULL,
                    PRIMARY KEY(ID)
                );

                CREATE TABLE IF NOT EXIST groups
                (
                    ID INT(11) NOT NULL AUTO_INCREMENT,
                    NAME TEXT NOT NULL,
                    PARENT_GID INT(11) NOT NULL,
                    PERMISSIONS TEXT NOT NULL,
                    PRIMARY KEY(ID)
                );
            ");
        }

        public static void isBanned(Player player, Action<PlayerData, bool> callback)
        {
            /// TODO: Check if is banned.

            PlayerData ply = findPlayerData(player);

            if (ply != null)
            {
                MySQL.FetchAllAsync("SELECT * FROM bans WHERE BANNED_PID = '" + ply.getUID() + "'", new Dictionary<string, string>(), new Action<DataTable>((data) =>
                {
                    bool isBanned = false;
                    foreach (DataRow result in data.Rows)
                    {
                        if (result[3].ToString() != "Permanently")
                        {
                            DateTime releaseDate = DateTime.Parse(result[3].ToString());
                            DateTime currentDate = DateTime.Now;

                            if (
                                (
                                    releaseDate.Year < currentDate.Year ||
                                    (releaseDate.Day < currentDate.Day && releaseDate.Month < currentDate.Month)
                                ) &&
                                (
                                    releaseDate.Second < currentDate.Second &&
                                    releaseDate.Minute < currentDate.Minute &&
                                    releaseDate.Hour < currentDate.Hour
                            ))
                            {
                                isBanned = true;
                                break;
                            }
                        }
                        else
                        {
                            isBanned = true;
                        }
                    }

                    callback?.Invoke(ply, isBanned);
                }));
            }
            else
            {
                callback?.Invoke(ply, false);
            }
        }
        public static void isBanned(PlayerData player, Action<PlayerData, bool> callback)
        {
            isBanned(player.getHandler(), callback);
        }

        public static void isWhitelisted(Player player, Action<PlayerData, bool> callback)
        {
            PlayerData ply = findPlayerData(player);

            if (ply != null)
            {
                MySQL.FetchAllAsync("SELECT * FROM whitelists WHERE WHITELIST_PID = '" + ply.getUID() + "'", new Dictionary<string, string>(), new Action<DataTable>((data) =>
                {
                    callback?.Invoke(ply, (data.Rows.Count > 0));
                }));

                return;
            }

            callback?.Invoke(ply, false);
        }
        public static void isWhitelisted(PlayerData player, Action<PlayerData, bool> callback)
        {
            isWhitelisted(player, callback);
        }

        public static void getBannedMessage(Player player, bool withDate, bool withBannersName, Action<PlayerData, string> callback)
        {
            PlayerData ply = findPlayerData(player);

            MySQL.FetchAllAsync("SELECT * FROM bans WHERE BANNED_PID = '" + ply.getUID() + "'", new Dictionary<string, string>(), new Action<DataTable>((data) =>
            {
                if(data.Rows.Count > 0)
                {
                    DataRow row = data.Rows[0];
                    callback?.Invoke(ply, row["REASON"].ToString());
                }
            }));
        }

        public static void getBannedMessage(PlayerData player, bool withDate, bool withBannersName, Action<PlayerData, string> callback)
        {
            getBannedMessage(player.getHandler(), withDate, withBannersName, callback);
        }

        public static void getUserGroup(PlayerData player, Action<PlayerData, Group> callback)
        {
            Group group = Main.groups[0];
        }

        public static void getRoles(PlayerData player, Action<PlayerData, Group> callback)
        {
            Role role = null;

        }

        public static PlayerData findPlayerData(Player player)
        {
            return findPlayerData(player.Handle, false);
        }

        public static PlayerData findPlayerData(string player, bool isUsername)
        {
            foreach (PlayerData data in Main.players)
            {
                if(isUsername)
                {

                    if (data.getUserName() == player)
                    {
                        return data;
                    }
                }
                else
                {
                    if (data.getHandler().Handle == player)
                    {
                        return data;
                    }
                }
            }

            return null;
        }
    }
}
