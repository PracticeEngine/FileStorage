using System;
using System.Configuration;

namespace PE.Storage.FileSystem
{
    public class FileStorageOptions
    {
        public string RootPath { get; set; }

        /// <summary>
        /// Static Method that attempts to create <see cref="FileStorageOptions"/> from the Default Configuration Manager
        /// </summary>
        /// <returns></returns>
        public static FileStorageOptions CreateFromDefaultSettings()
        {
            var rootPath = ConfigurationManager.AppSettings["PEStorage:RootPath"];
            if (String.IsNullOrWhiteSpace(rootPath))
            {
                return null;
            }
            return new FileStorageOptions
            {
                RootPath = rootPath
            };
        }
    }
}
