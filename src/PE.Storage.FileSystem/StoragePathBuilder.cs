using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage.FileSystem
{
    public class StoragePathBuilder
    {
        private readonly string _rootPath;

        /// <summary>
        /// Constructs a StoragePathBuilder
        /// </summary>
        /// <param name="rootPath"></param>
        public StoragePathBuilder(string rootPath)
        {
            _rootPath = rootPath.TrimEnd('\\') + "\\";
        }

        /// <summary>
        /// Calculates a Blob's Storage Folder
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public string GetStoreFolder(string store)
        {
            return _rootPath + "\\" + store.Trim('\\') + "\\";
        }

        /// <summary>
        /// Returns a Blob's Id value
        /// </summary>
        /// <param name="store"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public string GetBlobId(string store, string blob)
        {
            return store.Trim('\\') + "\\" + blob.Trim('\\');
        }

        /// <summary>
        /// Calculates a Blob's Json (Metadata) File
        /// </summary>
        /// <param name="store"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public string GetJsonFile(string store, string blob)
        {
            return _rootPath + "\\" + GetBlobId(store, blob) + ".json";
        }

        /// <summary>
        /// Calculates a Blob's Data (Storage) File
        /// </summary>
        /// <param name="store"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public string GetBlobFile(string store, string blob)
        {
            return _rootPath + "\\" + GetBlobId(store, blob) + ".blob";
        }
    }
}
