using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XAdminSystem.Core.Data
{
    public class Group : Permission
    {
        new protected string name;
        protected Group inheritance_parent;
        protected List<Permission> permissions; 
        protected bool editible = true;

        public Group(string name, bool editible = true) : base(name)
        {
            this.inheritance_parent = null;
            this.editible = editible;
            this.permissions = new List<Permission>();
        }

        public void setParent(Group parent) => this.inheritance_parent = parent;


        public void setPermissions(List<Permission> permissions) => this.permissions = permissions;

        public void addPermission(Permission permission) => permissions.Add(permission);
        
        new public string getName()
        {
            return name;
        }

        public Group getParent()
        {
            return this.inheritance_parent;
        }

        public List<Permission> getPermissions()
        {
            return this.permissions;
        }

        public bool isEditible()
        {
            return this.editible;
        }
    }
}
