using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage.FileSystem
{
    public class FileSystemBlobProvider : IBlobStorage
    {
        private readonly FileStorageOptions _options;
        private readonly StoreManager _manager;
        private readonly StoragePathBuilder _pathBuilder;

        /// <summary>
        /// Constructor that builds Options from Default Settings
        /// </summary>
        FileSystemBlobProvider() : this(FileStorageOptions.CreateFromDefaultSettings())
        {

        }

        /// <summary>
        /// Constructor that uses provided settings
        /// </summary>
        /// <param name="options"></param>
        FileSystemBlobProvider(FileStorageOptions options)
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
            _manager = new StoreManager();
            _pathBuilder = new StoragePathBuilder(rootDirectory);
        }

        /// <summary>
        /// Creates a new blob in the file storage area
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public async Task<string> CreateAsync(PEStorageBlob blob, Stream data)
        {
            var store = _manager.GetRandomStoreFolder();
            var storePath = _pathBuilder.GetStoreFolder(store);
            var newBlob = await _manager.NextAvailableFile(storePath);
            var blobMeta = _pathBuilder.GetJsonFile(store, newBlob);
            var blobData = _pathBuilder.GetBlobFile(store, newBlob);
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
                await data.CopyToAsync(dataStream);
                await dataStream.FlushAsync();
            }
            return newBlob;
        }

        /// <summary>
        /// Deletes a blob in the File Storage Area
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task DeleteAsync(string Id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a Blob from the File System
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<PEStorageBlob> GetBlobAsync(string Id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a Stream to access the data in the blob
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<Stream> GetDataAync(string Id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a blob in the file storage area
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task UpdateAsync(string Id, PEStorageBlob blob, Stream data = null)
        {
            throw new NotImplementedException();
        }
    }
}
