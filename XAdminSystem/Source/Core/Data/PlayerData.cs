using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core;
using XAdminSystem.Core.Database;
using Newtonsoft.Json;
using System.Data;

namespace XAdminSystem.Core.Data
{
    public class PlayerData
    {
        private int UID;
        private Player handle;
        private Group userGroup;
        private List<Role> roles = new List<Role>();
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private List<Permission> permissions = new List<Permission>();

        private string originalUserName;

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

        public async void BanAsync(PlayerData banner, string reason = "No Reason Given.", string timeString = "perma")
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("@bannedID", this.getUID().ToString());
            tags.Add("@bannerID", (banner == null ? "SYSTEM" : banner.getUID().ToString()));
            tags.Add("@reason", reason);

            DateTime expireDate = DateTime.Now;

            if (timeString != "perma")
            {
                List<string> dateSeconds = new List<string> { };
                List<string> dateMinutes = new List<string> { };
                List<string> dateHours = new List<string> { };
                List<string> dateDays = new List<string> { };
                List<string> dateWeeks = new List<string> { };
                List<string> dateMonths = new List<string> { };
                List<string> dateYears = new List<string> { };

                dateSeconds.AddRange(timeString.Split('s'));
                dateMinutes.AddRange(timeString.Split('m'));
                dateHours.AddRange(timeString.Split('h'));
                dateDays.AddRange(timeString.Split('d'));
                dateWeeks.AddRange(timeString.Split('w'));
                dateMonths.AddRange(timeString.Split('M'));
                dateYears.AddRange(timeString.Split('y'));

                dateSeconds.ForEach((seconds) => expireDate.AddSeconds(Convert.ToInt32(seconds)));
                dateMinutes.ForEach((minutes) => expireDate.AddMinutes(Convert.ToInt32(minutes)));
                dateHours.ForEach((hours) => expireDate.AddHours(Convert.ToInt32(hours)));
                dateDays.ForEach((days) => expireDate.AddDays(Convert.ToInt32(days)));
                dateWeeks.ForEach((weeks) => expireDate.AddDays(Convert.ToInt32(weeks) * 7));
                dateMonths.ForEach((months) => expireDate.AddMonths(Convert.ToInt32(months)));
                dateYears.ForEach((years) => expireDate.AddYears(Convert.ToInt32(years)));
            }
            
            tags.Add("@expireDate", (timeString == "perma" ? "perma" : expireDate.ToString()));

            tags.Add("@createdDate", DateTime.Now.ToString());

            await MySQL.ExecuteSQLAsync(@"
                INSERT INTO bans 
                (
                    BANNED_PID,
                    BANNER_PID,
                    RELEASE_DATE,
                    CREATION_DATE
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
                );", tags);

            this.Kick(reason);
        }

        public void checkDatabase()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("@steamID", this.getIdentifier());

            MySQL.FetchAllAsync("SELECT * FROM players WHERE STEAMID_ADDRESS = '@steamID'", tags, new Action<DataTable>((data) => 
            {
                if(data.Rows[0] != null)
                {
                    this.load();
                }
                else
                {
                    this.create();
                }
            }));
        }

        public async void create()
        {

            Dictionary<string, string> tags = new Dictionary<string, string>();

            tags.Add("@username", this.getUserName());
            tags.Add("@othernames", JsonConvert.SerializeObject(new List<string> { this.getUserName() }));
            tags.Add("@ipaddresses", JsonConvert.SerializeObject(new List<string> { this.getIPAddress() }));
            tags.Add("@steamids", this.getIdentifier());
            tags.Add("@group", this.getUserGroup().getName());

            List<string> roles = new List<string>();
            foreach(Role role in this.getRoles())
            {
                roles.Add(role.getName());
            }
            tags.Add("@roles", JsonConvert.SerializeObject(roles));

            await MySQL.ExecuteSQLAsync(@"
                INSERT INTO players 
                (
                    USERNAME,
                    OTHER_USERNAMES,
                    IP_ADDRESSES,
                    STEAMID_ADDRESS,
                    GROUP,
                    ROLES
                ) 
                VALUES 
                (
                    '@username',
                    '@othernames',
                    '@ipaddresses',
                    '@steamid',
                    '@group',
                    '@roles'
                );", tags);

            this.load();
        }

        public void load()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("@steamID", this.getIdentifier());

            MySQL.FetchAllAsync("SELECT * FROM players WHERE STEAMID_ADDRESS = '@steamID'", tags, new Action<DataTable>((data) =>
            {
                if (data.Rows[0] != null)
                {
                    DataRow row = data.Rows[0];

                    {
                        List<string> names = (List<string>)JsonConvert.DeserializeObject(row[2].ToString());

                        if(names[0] != this.getUserName())
                        {
                            
                        }
                    }

                    #region Load Roles
                    List<string> roles = (List<string>)JsonConvert.DeserializeObject(row[6].ToString());
                    List<Role> oRoles = new List<Role>();

                    foreach(Role role in Main.roles)
                    {
                        if (RoleContains(role.getName(), roles))
                        {
                            oRoles.Add(role);
                        }
                    }
                    #endregion



                }
            }));
        }

        private bool RoleContains(string role, List<string> otherroles)
        {
            foreach (string orole in otherroles)
            {
                if (orole == role)
                {
                    return true;
                }
            }

            return false;
        }

        private bool RoleContains(string role, List<Role> otherroles)
        {
            List<string> roles = new List<string>();
            foreach(Role r in otherroles)
            {
                roles.Add(r.getName());
            }
            return RoleContains(role, roles);
        }

        private bool RoleContains(Role role, List<Role> otherroles)
        {
            return RoleContains(role.getName(), otherroles);
        }

        public async void save()
        {
            Stack<string> otherusernames = new Stack<string>();
            List<string> otherroles_names = new List<string>();
            Stack<string> otherIPAddresses = new Stack<string>();
            string Group = this.getUserGroup().getName();
            {

                MySQL.FetchAllAsync("SELECT * FROM players WHERE STEAMID_ADDRESS = '" + this.getIdentifier() + "'", null, new Action<System.Data.DataTable>((data) =>
                  {
                      foreach (DataRow result in data.Rows)
                      {
                          otherusernames = (Stack<string>)JsonConvert.DeserializeObject(result[2].ToString());
                          otherIPAddresses = (Stack<string>)JsonConvert.DeserializeObject(result[3].ToString());
                          otherroles_names = (List<string>)JsonConvert.DeserializeObject(result[6].ToString());
                      }
                  }));

                #region Handle UserNames
                {
                    bool foundUserName = false;
                    foreach (string username in otherusernames)
                    {
                        if (username == this.getUserName())
                        {
                            foundUserName = true;
                            break;
                        }
                    }

                    // This will set the Username at the top of the List.
                    if (!foundUserName)
                        otherusernames.Push(this.getUserName());
                }
                #endregion

                #region Handle Roles
                {
                    // Convert from Database to RoleList
                    List<Role> otherroles = new List<Role>();
                    foreach (Role roleObj in Main.roles)
                    {
                        foreach (string role in otherroles_names)
                        {
                            if (roleObj.getName() == role)
                            {
                                otherroles.Add(roleObj);
                            }
                        }
                    }

                    foreach (Role role in this.getRoles())
                    {
                        if (!RoleContains(role, otherroles))
                        {
                            otherroles.Add(role);
                        }
                    }

                    otherroles_names.Clear();
                    otherroles_names = new List<string>();

                    // Convert from RoleList to Database
                    roles.ForEach((role) => otherroles_names.Add(role.getName()));
                }
                #endregion

                #region Handle IPAddresses
                {
                    bool foundIPAddress = false;
                    foreach (string ipAddress in otherIPAddresses)
                    {
                        if (ipAddress == this.getIPAddress())
                        {
                            foundIPAddress = true;
                            break;
                        }
                    }

                    // This will set the current IPAddress to the top of the list.
                    if (!foundIPAddress)
                        otherusernames.Push(this.getIPAddress());
                }
                #endregion

            }

            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("@usernames", JsonConvert.SerializeObject(otherusernames));
            tags.Add("@ipAddresses", JsonConvert.SerializeObject(otherIPAddresses));
            tags.Add("@group", Group);
            tags.Add("@roles", JsonConvert.SerializeObject(otherroles_names));
            tags.Add("@steamID", this.getIdentifier());

            await MySQL.ExecuteSQLAsync(@"
                UPDATE players SET
                OTHER_USERNAMES = '@usernames',
                IP_ADDRESSES = '@ipAddresses',
                GROUP = '@group',
                ROLES = '@roles'
                WHERE STEAMID_ADDRESS = '@steamID'
            ", tags);
        }

        public void SendMessage(string message, string title = "")
        {
             BaseScript.TriggerClientEvent(this.getHandler(), "chatMessage", title, new[] { 255, 255, 255 }, message);
        }

        public void setRoles(List<Role> list)
        {
            roles = list;
        }

        public int getUID()
        {
            return UID;
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

        public string getOriginalUserName()
        {
            return this.originalUserName;
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

        public string getIPAddress()
        {
            return handle.EndPoint;
        }

        public Group getUserGroup()
        {
            return userGroup;
        }

        public List<Role> getRoles()
        {
            return roles;
        }

        public bool hasPermission(string permission)
        {
            foreach (Permission perm in permissions)
            {
                if (perm.getName() == permission)
                {
                    return true;
                }
            }

            return false;
        }

        public bool hasPermission(Permission obj)
        {
            return hasPermission(obj.getName());
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
