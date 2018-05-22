using System.Configuration;

namespace PE.Storage.BlobTable
{
    public class BlobTableOptions
    {
        public string DatabaseConnectionString { get; set; }

        /// <summary>
        /// Static Method that attempts to create <see cref="BlobTableOptions"/> from the Default Configuration Manager
        /// </summary>
        /// <returns></returns>
        public static BlobTableOptions CreateFromDefaultSettings()
        {
            var connString = ConfigurationManager.ConnectionStrings["EngineDb"];
            if (connString == null)
            {
                return null;
            }
            return new BlobTableOptions
            {
                DatabaseConnectionString = connString.ConnectionString
            };
        }
    }
}
