using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAdminSystem.Core.Data;

namespace XAdminSystem.Core.Database
{
    public class AdminDB
    {
        public static void setupDatabase()
        {
        }

        public static bool isBanned(Player player)
        {
            return false;
        }
        public static bool isBanned(PlayerData player)
        {
            return isBanned(player.getHandler());
        }

        public static bool isWhitelisted(Player player)
        {
            return false;
        }
        public static bool isWhitelisted(PlayerData player)
        {
            return isWhitelisted(player.getHandler());
        }

        public static Group getUserGroup(PlayerData player)
        {
            return null;
        }

        public static List<Role> getRoles(PlayerData player)
        {
            return null;
        }
    }
}
