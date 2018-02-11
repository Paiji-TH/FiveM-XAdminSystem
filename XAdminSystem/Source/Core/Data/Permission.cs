using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XAdminSystem.Core.Data
{
    public class Permission
    {
        protected string name;
        protected bool hasAccess = false;
        protected List<Group> restricted = new List<Group>();

        public Permission(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Sets the permissions name.
        /// </summary>
        /// <param name="name"></param>
        public void setName(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Sets the accessibility of the permission, either it's allowed or disabled.
        /// </summary>
        /// <param name="val"></param>
        public void setAccessibility(bool val)
        {
            hasAccess = val;
        }

        /// <summary>
        /// Sets the list of groups, it cannot touch.
        /// </summary>
        /// <param name="restrictions"></param>
        public void setRestriction(List<Group> restrictions)
        {
            this.restricted = restrictions;
        }

        /// <summary>
        /// Adds a group to the restricted groups, that it cannot touch.
        /// </summary>
        /// <param name="group"></param>
        public void addRestrictedGroup(Group group)
        {
            this.restricted.Add(group);
        }

        /// <summary>
        /// This will return the name of the permission.
        /// </summary>
        /// <returns></returns>
        public string getName()
        {
            return name;
        }

        /// <summary>
        /// This will return, if the permission has been given or disabled.
        /// </summary>
        /// <returns></returns>
        public bool getAccessiblity()
        {
            return hasAccess;
        }

        /// <summary>
        /// This will return the groups that this permission cannot touch.
        /// </summary>
        /// <returns></returns>
        public List<Group> getRestrictedGroups()
        {
            return restricted;
        }
    }
}
