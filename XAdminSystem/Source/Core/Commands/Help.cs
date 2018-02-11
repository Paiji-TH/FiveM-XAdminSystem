using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using XAdminSystem.Core.Data;

namespace XAdminSystem.Core.Commands
{
    public class Help : ChatCommand
    {
        public Help() : base("help", "Displays all the commands.", null)
        {
            this.runCommand += Run;
        }

        private void Run(string[] args)
        {
            int page = Convert.ToInt32(args[0]);

            BaseScript.TriggerClientEvent("chatMessage", "", new[] { 255, 255, 255 }, "Page: " + page + "/" + Math.Round((double)(Main.commands.Count / 5)));
            BaseScript.TriggerClientEvent("chatMessage", "", new[] { 255, 255, 255 }, "+==================================================+");

            for (int i = 0; i < 5; i++)
            {
                ChatCommand cmd = Main.commands[i * page];
                BaseScript.TriggerClientEvent("chatMessage", "", new[] { 255, 255, 255 }, cmd.getCommandText() + " - " + cmd.getDescription());
            }
            BaseScript.TriggerClientEvent("chatMessage", "", new[] { 255, 255, 255 }, "+==================================================+");
        }
    }
}
