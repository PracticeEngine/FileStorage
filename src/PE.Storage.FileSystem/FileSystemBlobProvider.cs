using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PE.Storage.FileSystem
{
    public class FileSystemBlobProvider : IBlobStorage
    {
        private readonly FileStorageOptions _options;
        private readonly StoreManager _manager;

        /// <summary>
        /// Constructor that builds Options from Default Settings
        /// </summary>
        public FileSystemBlobProvider() : this(FileStorageOptions.CreateFromDefaultSettings())
        {

        }

        /// <summary>
        /// Constructor that uses provided settings
        /// </summary>
        /// <param name="options"></param>
        public FileSystemBlobProvider(FileStorageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            var rootDirectory = Path.GetDirectoryName(options.RootPath.TrimEnd('\\')+'\\');
            if (!Directory.Exists(rootDirectory))
            {
                Directory.CreateDirectory(rootDirectory);
            }
            options.RootPath = rootDirectory;
            _options = options;
            _manager = new StoreManager(rootDirectory);
        }

        /// <summary>
        /// Creates a new blob in the file storage area
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public async Task<string> CreateAsync(PEStorageBlob blob, Stream data)
        {
            var blobId = await _manager.GetStoreDb().NextAvailableFile();
            var blobMeta = _manager.GetMetaPath(blobId);
            var blobData = _manager.GetDataPath(blobId);
            var blobPath = Path.GetDirectoryName(blobMeta);
            if (!Directory.Exists(blobPath))
            {
                Directory.CreateDirectory(blobPath);
            }
            var json = JsonConvert.SerializeObject(blob, Formatting.None);
            File.WriteAllText(blobMeta, json);
            if (data.CanSeek && data.Position > 0)
            {
                data.Seek(0, SeekOrigin.Begin);
            }
            using (FileStream dataStream = File.Open(blobData, FileMode.CreateNew, FileAccess.Write))
            {
                await data.CopyToAsync(dataStream).ConfigureAwait(false);
                await dataStream.FlushAsync().ConfigureAwait(false);
            }
            return blobId;
        }

        /// <summary>
        /// Deletes a blob in the File Storage Area
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task DeleteAsync(string Id)
        {
            var blobMeta = _manager.GetMetaPath(Id);
            var blobData = _manager.GetDataPath(Id);
            if (File.Exists(blobMeta))
                File.Delete(blobMeta);
            if (File.Exists(blobData))
                File.Delete(blobData);
            // Cleanup unused Directories
            var path = new DirectoryInfo(Path.GetDirectoryName(blobMeta));
            var hasFiles = path.GetFileSystemInfos().Length > 0;
            while (!hasFiles)
            {
                path.Delete();
                path = path.Parent;
                hasFiles = path.GetFileSystemInfos().Length > 0;
            }
            // Remove the Store Folder if all files have been removed
            if (path.GetFileSystemInfos().Length == 1)
            {
                if (path.GetFiles("store.lock").Length == 1)
                {
                    path.Delete(true);
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a Blob from the File System
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<PEStorageBlob> GetBlobAsync(string Id)
        {
            var blobMeta = _manager.GetMetaPath(Id);
            var metaJson = File.ReadAllText(blobMeta);
            var metaData = JsonConvert.DeserializeObject<PEStorageBlob>(metaJson);
            return Task.FromResult(metaData);
        }

        /// <summary>
        /// Returns a Stream to access the data in the blob
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<Stream> GetDataAync(string Id)
        {
            var blobData = _manager.GetDataPath(Id);
            var memoryStream = new MemoryStream();
            using (var stream = File.Open(blobData, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Updates a blob in the file storage area
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string Id, PEStorageBlob blob, Stream data = null)
        {
            var blobMeta = _manager.GetMetaPath(Id);
            var metaJson = JsonConvert.SerializeObject(blob, Formatting.None);
            File.WriteAllText(blobMeta, metaJson);
            if (data != null)
            {
                var blobData = _manager.GetDataPath(Id);
                using (var fileStream = File.Open(blobData, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    await data.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }
    }
}
