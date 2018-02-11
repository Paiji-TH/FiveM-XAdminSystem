using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core;
using XAdminSystem.Core.Database;

namespace XAdminSystem.Core.Data
{
    public class PlayerData
    {
        private Player handle;
        private Group userGroup;
        private List<Role> roles = new List<Role>();
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private List<Permission> permissions = new List<Permission>();

        public PlayerData(Player handle, Group userGroup = null)
        {
            this.handle = handle;

            if (userGroup != null)
                this.userGroup = userGroup;
            else
                this.userGroup = Main.groups[0];

            API.ExecuteCommand("add_principal identifier." + handle.Identifiers["steam"] + " group." + userGroup.getName());
        }

        public void Kick(string reason = "No Reason Given.")
        {
            handle.Drop(reason);
        }

        public async void BanAsync(string reason = "No Reason Given.", string timeString = "perma")
        {
            await MySQL.ExecuteSQLAsync(@"
                INSERT INTO bans 
                (
                    bannedID, 
                    bannerID, 
                    reason, 
                    expireDate, 
                    createdDate, 
                    bannedUserName, 
                    bannerUserName
                ) 
                VALUES 
                (
                    '@bannedID', 
                    '@bannerID', 
                    '@reason',
                    '@expireDate',
                    '@createdDate',
                    '@bannedUserName',
                    '@bannerUserName'
                )");

            this.Kick(reason);
        }

        public void setRoles(List<Role> list)
        {
            roles = list;
        }

        public void addRole(Role role)
        {
            roles.Add(role);
        }

        public void addRole(List<Role> role)
        {
            roles.AddRange(role);
        }

        public void setUserGroup(Group group)
        {
            if (group != null)
                this.userGroup = group;
            else
                this.userGroup = Main.groups[0];
        }

        public string getUserName()
        {
            return handle.Name;
        }

        public int getPing()
        {
            return handle.Ping;
        }

        public int getLastMessage()
        {
            return handle.LastMsg;
        }

        public string getIdentifier()
        {
            return handle.Identifiers["steam"];
        }

        public Group getUserGroup()
        {
            return userGroup;
        }

        public List<Role> getRoles()
        {
            return roles;
        }

        public bool hasPermission(Permission obj)
        {
            foreach(Permission perm in permissions)
            {
                if(perm.getName() == obj.getName())
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, object> getAllData()
        {
            return data;
        }

        public Player getHandler()
        {
            return handle;
        }

    }
}
