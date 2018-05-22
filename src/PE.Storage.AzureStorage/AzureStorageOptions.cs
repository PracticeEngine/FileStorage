using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage.AzureStorage
{
    public class AzureStorageOptions
    {
        public string AccountConnectionString { get; set; }

        /// <summary>
        /// Static Method that attempts to create FileStorageOptions from the Default Configuration Manager
        /// </summary>
        /// <returns></returns>
        public static AzureStorageOptions CreateFromDefaultSettings()
        {
            var connString = ConfigurationManager.ConnectionStrings["PEStorageConnectionString"];
            if (connString == null)
            {
                return null;
            }
            return new AzureStorageOptions
            {
                AccountConnectionString = connString.ConnectionString
            };
        }
    }
}
