using System.Configuration;

namespace PE.BlobStorage.AzureStorage
{
    public class AzureStorageOptions
    {
        public string AccountConnectionString { get; set; }

        /// <summary>
        /// Static Method that attempts to create <see cref="AzureStorageOptions"/> from the Default Configuration Manager
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
