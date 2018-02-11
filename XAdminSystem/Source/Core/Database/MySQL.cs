using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using CitizenFX.Core;

namespace XAdminSystem.Core.Database
{
    public class MySQL
    {
        private static MySqlConnection con;

        /// <summary>
        /// Sets up the connection data between the Administrative System to the Database.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="portAddress"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="database"></param>
        public static async Task ConnectAsync(string ipAddress, int portAddress, string username, string password, string database)
        {
            con = new MySqlConnection("datasource="+ipAddress+";port="+portAddress+";user="+ username +";password="+ password + ";database=" + database);
            await con.OpenAsync();
        }

        /// <summary>
        /// Executes an SQL command to the MySQL Database.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        public static async Task ExecuteSQLAsync(string sql, Dictionary<string, string> parameters = null)
        {
            if (con == null || con.State == System.Data.ConnectionState.Closed) return;

            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> replacement in parameters)
                {
                    sql.Replace(replacement.Key, replacement.Value);
                }
            }

            MySqlCommand command = new MySqlCommand(sql, con);
            await command.ExecuteReaderAsync();
        }

        /// <summary>
        /// Fetchs all the data from the SQL Request.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        public static async void FetchAllAsync(string sql, Dictionary<string, string> parameters = null, Action<DataTable> callback = null)
        {
            if (con == null || con.State == ConnectionState.Closed) return;

            if(parameters != null)
            {
                foreach(KeyValuePair<string, string> replacement in parameters)
                {
                    sql.Replace(replacement.Key, replacement.Value);
                }
            }

            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                try
                {
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable data = new DataTable();
                    await adapter.FillAsync(data);

                    callback?.Invoke(data);
                }
                catch (MySqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message.ToString());
                    Console.ResetColor();
                }
            }
        }
    }
}
