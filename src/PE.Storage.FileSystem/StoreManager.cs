using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PE.Storage.FileSystem
{
    /// <summary>
    /// Calculates File Path for a file to be stored in a random distribution of 500 root folders.  
    /// We use the 500 root to support 500 lock files (allowing relatively high conconcurrency)
    /// Within each random root store folder, it stores in 3 folders deep (1,000 x 1,000 x 500 files) = 500,000,000 files per store
    /// That allows 250,000,000,000 files to be stored.  This won't be a perfectly even distribution, but should push things way out into the future before the first store is filled
    /// This seems excessive, but it's a forward only (we don't go back and re-use in writes after deletes)
    /// </summary>
    public class StoreManager
    {
        const int ROOT_FOLDER_COUNT = 500;
        const int STORE_FOLDER_COUNT = 1000;
        const int STORE_FILE_COUNT = 500;
        private readonly Random random;

        public StoreManager()
        {
            random = new Random();
        }

        /// <summary>
        /// Provides a randomly selected top (store-folder)
        /// </summary>
        /// <returns>Store Folder Name (e.g. 0001)</returns>
        public string GetRandomStoreFolder()
        {
            return random.Next(1, ROOT_FOLDER_COUNT).ToString("X4");
        }

        /// <summary>
        /// Provides The next filename in the selected store.
        /// </summary>
        /// <param name="storeFolder">Full path to the Store Folder</param>
        /// <returns>Path to File NOT including store Folder</returns>
        public async Task<string> NextAvailableFile(string storeFolder)
        {
            // Names for the folders in the store
            int[] fileValue = new[] { 1, 1, 1 };
            string lockFile = Path.Combine(storeFolder, "store.lock");
            using (FileStream lockStream = File.Open(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                // If there is data
                if (lockStream.Length > 0)
                {
                    await ReadValuesFromLockFile(fileValue, lockStream).ConfigureAwait(false);
                }
                // Calculate the next File
                CalculateNextFileValue(fileValue);
                // Write the file while it's still locked
                await WriteValuesToLockFile(fileValue, lockStream).ConfigureAwait(false);
            }

            var pathParts = fileValue.Select(fv => fv.ToString("X4"));
            return Path.GetDirectoryName(storeFolder)String.Join("\\",pathParts);
        }

        /// <summary>
        /// Calculates the next File Value (aka Folders and Name)
        /// </summary>
        /// <param name="structure"></param>
        private static void CalculateNextFileValue(int[] structure)
        {
            if (structure[structure.Length - 1] == STORE_FILE_COUNT)
            {
                // Folder is full, set to file 1 and create next folder up the tree
                structure[structure.Length - 1] = 1;
                for (int store = structure.Length - 2; store >= 0; store--)
                {
                    if (structure[store] == STORE_FOLDER_COUNT)
                    {
                        // Set this folder to 1 and move up a level
                        structure[store] = 1;
                        continue;
                    }
                    else
                    {
                        // Just increment and we are done
                        structure[store]++;
                        break;
                    }
                }
            }
            else
            {
                // Just increment the filename
                structure[structure.Length - 1]++;
            }
        }

        /// <summary>
        /// Writes Values to the Lock File
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="lockStream"></param>
        /// <returns></returns>
        private static async Task WriteValuesToLockFile(int[] structure, FileStream lockStream)
        {
            using (StreamWriter writer = new StreamWriter(lockStream))
            {
                for (int s = 0; s < structure.Length; s++)
                {
                    await writer.WriteLineAsync(structure[s].ToString("X4")).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Reads Values from the Lock File
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="lockStream"></param>
        /// <returns></returns>
        private static async Task ReadValuesFromLockFile(int[] structure, FileStream lockStream)
        {
            using (StreamReader reader = new StreamReader(lockStream))
            {
                for (int s = 0; s < structure.Length; s++)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    int x;
                    if (Int32.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out x))
                    {
                        structure[s] = x;
                    }
                }
            }
        }
    }
}
