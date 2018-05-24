using System;
using System.IO;

namespace PE.BlobStorage.FileSystem
{
    /// <summary>
    /// Calculates File Path for a file to be stored in a random distribution of 500 root folders.  
    /// We use the 500 root to support 500 lock files (allowing relatively high conconcurrency)
    /// Within each random root store folder, it stores in 3 folders deep (1,000 x 1,000 x 500 files) = 500,000,000 files per store
    /// That allows 250,000,000,000 files to be stored.  This won't be a perfectly even distribution, but should push things way out into the future before the first store is filled
    /// This seems excessive, but it's a forward only (we don't go back and re-use names after deletes)
    /// </summary>
    public class StoreManager
    {
        const int ROOT_FOLDER_COUNT = 500;
        private readonly Random random;
        private readonly string _rootPath;

        public StoreManager(string rootPath)
        {
            random = new Random();
            _rootPath = rootPath.TrimEnd('\\') + "\\";
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        /// <summary>
        /// Provides a randomly selected top (store-folder)
        /// </summary>
        /// <returns>Store Folder Name (e.g. 0001)</returns>
        public FileDb GetStoreDb()
        {
            var randomStore = random.Next(1, ROOT_FOLDER_COUNT).ToString("X4");
            return GetStoreDb(randomStore);
        }

        /// <summary>
        /// Returns a FileStore
        /// </summary>
        /// <param name="nameOrId">name of Store or Id of a Blob</param>
        /// <returns></returns>
        public FileDb GetStoreDb(string nameOrId)
        {
            // Normalize Name to get first Path part as StoreName
            var storeName = nameOrId.Trim('\\');
            storeName = storeName.Contains("\\") ? storeName.Substring(0, storeName.IndexOf('\\')) : storeName;
            var storeRoot = Path.Combine(_rootPath, storeName);
            if (!Directory.Exists(storeRoot))
                Directory.CreateDirectory(storeRoot);
            return new FileDb(storeRoot);
        }

        /// <summary>
        /// Returns path to a metadata file (.json)
        /// </summary>
        /// <param name="id">Blob Id</param>
        /// <returns></returns>
        public string GetMetaPath(string id)
        {
            return Path.Combine(_rootPath, id + ".json");
        }

        /// <summary>
        /// Returns path to a metadata file (.blob)
        /// </summary>
        /// <param name="id">Blob Id</param>
        /// <returns></returns>
        public string GetDataPath(string id)
        {
            return Path.Combine(_rootPath, id + ".blob");
        }

    }
}
