using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PE.BlobStorage.FileSystem
{
    /// <summary>
    /// Operates over a specific folder with a store.lock in the root which is used for naming logic
    /// </summary>
    public class FileDb
    {
        private readonly string _storePath;
        private readonly string _lockFile;
        const int STORE_FOLDER_COUNT = 1000;
        const int STORE_FILE_COUNT = 500;

        /// <summary>
        /// Constructs a File Store
        /// </summary>
        /// <param name="storePath"></param>
        public FileDb(string storePath)
        {
            _storePath = storePath.TrimEnd('\\') + '\\';
            if (!Directory.Exists(_storePath))
                throw new DirectoryNotFoundException("FileDb must point to an existing Directory!");
            _lockFile = Path.Combine(_storePath, "store.lock");
        }

        /// <summary>
        /// Provides The next filename in the selected store.
        /// </summary>
        /// <param name="storeFolder">Full path to the Store Folder</param>
        /// <returns>File Id</returns>
        public async Task<string> NextAvailableFile()
        {
            // Names for the folders in the store
            int[] fileValue = new[] { 1, 1, 0 };

            // Retry logic over exclusive file lock
            for (int numTries = 3; numTries > 0; numTries--)
            {
                try
                {
                    using (FileStream lockStream = File.Open(_lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If there is data
                        if (lockStream.Length > 0)
                        {
                            await ReadValuesFromLockFile(fileValue, lockStream).ConfigureAwait(false);
                        }
                        // Calculate the next File
                        CalculateNextFileValue(fileValue);
                        // Write the file while it's still locked
                        lockStream.Seek(0, SeekOrigin.Begin);
                        await WriteValuesToLockFile(fileValue, lockStream).ConfigureAwait(false);
                    }
                    numTries = 0;
                }
                catch (IOException ioe)
                {
                    if (numTries == 0)
                        throw ioe;

                    Debug.WriteLine("lock file busy - waiting");
                    // Failed - wait 5ms, then retry
                    Thread.Sleep(5);
                }
            }
            var store = new DirectoryInfo(_storePath).Name;
            var pathParts = fileValue.Select(fv => fv.ToString("X4"));
            return store + "\\" + String.Join("\\", pathParts);
        }

        /// <summary>
        /// Calculates the next File Value (aka Folders and Name)
        /// </summary>
        /// <param name="structure"></param>
        public static void CalculateNextFileValue(int[] structure)
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
            using (StreamWriter writer = new StreamWriter(lockStream, Encoding.UTF8, 1024, true))
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
            using (StreamReader reader = new StreamReader(lockStream, Encoding.UTF8, false, 1024, true))
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
