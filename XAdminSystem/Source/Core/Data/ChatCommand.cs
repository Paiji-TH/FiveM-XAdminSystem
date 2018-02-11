using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace XAdminSystem.Core.Data
{
    public class ChatCommand : Permission
    {
        protected string description;
        protected Group group;

        public delegate void RunCommand(string[] args);
        protected event RunCommand runCommand;

        public ChatCommand(string name, string description, Group group = null) : base(name)
        {
            this.description = description;

            if (group != null)
                API.ExecuteCommand("add_ace group." + group.getName() + " command." + name + " allow");
        }

        public void DoCommand(string[] args)
        {
            runCommand?.Invoke(args);
        }

        public string getCommandText()
        {
            return name;
        }

        public string getDescription()
        {
            return description;
        }

        public bool canTarget(PlayerData caster, PlayerData target)
        {

            if (caster.hasPermission(this))
            {
                bool couldTarget = true;
                foreach(Group group in caster.getUserGroup().getRestrictedGroups())
                {
                    if (group.getName() == target.getUserGroup().getName())
                    {
                        couldTarget = false;
                        break;
                    }
                }

                if (couldTarget && caster.getUserGroup().getParent().getName() != target.getUserGroup().getName())
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
