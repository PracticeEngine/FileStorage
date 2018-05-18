using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage.FileSystem
{
    public class FileStorageOptions
    {
        public string RootPath { get; set; }

        /// <summary>
        /// Static Method that attempts to create FileStorageOptions from the Default Configuration Manager
        /// </summary>
        /// <returns></returns>
        public static FileStorageOptions CreateFromDefaultSettings()
        {
            var rootPath = ConfigurationManager.AppSettings["PE.Storage.RootPath"];
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
