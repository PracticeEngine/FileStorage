using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage.AzureStorage
{
    public class AzureStorageBlobProvider : IBlobStorage
    {
        private readonly AzureStorageOptions _options;
        private readonly CloudBlobClient _blobClient;
        private readonly CloudTableClient _tableClient;
        private static bool _containersCreated;

        /// <summary>
        /// Constructor that builds Options from Default Settings
        /// </summary>
        public AzureStorageBlobProvider() : this(AzureStorageOptions.CreateFromDefaultSettings())
        {

        }

        /// <summary>
        /// Constructor that uses provided settings
        /// </summary>
        /// <param name="options"></param>
        public AzureStorageBlobProvider(AzureStorageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _options = options;
            var account = CloudStorageAccount.Parse(options.AccountConnectionString);
            _blobClient = account.CreateCloudBlobClient();
            _tableClient = account.CreateCloudTableClient();
        }

        /// <summary>
        /// Ensures the structures are created to support the provider
        /// </summary>
        /// <returns></returns>
        private async Task EnsureContainers()
        {
            if (!_containersCreated)
            {
                _ = await _blobClient.GetContainerReference("thumbs").CreateIfNotExistsAsync();
                _ = await _blobClient.GetContainerReference("blobs").CreateIfNotExistsAsync();
                _ = await _tableClient.GetTableReference("blobmeta").CreateIfNotExistsAsync();
                _containersCreated = true;
            }
        }

        /// <summary>
        /// Creates a New Blob in Azure Storage
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> CreateAsync(PEStorageBlob blob, Stream data)
        {
            await EnsureContainers();
            // Create metadata
            var newBlob = MetaTable.NewEntity(blob);
            // Save Blob data
            var blobContainer = _blobClient.GetContainerReference("blobs");
            var blobStore = blobContainer.GetBlockBlobReference(newBlob.Id);
            if (data.CanSeek && data.Position > 0)
            {
                data.Seek(0, SeekOrigin.Begin);
            }
            await blobStore.UploadFromStreamAsync(data).ConfigureAwait(false);
            newBlob.BlobUri = blobStore.Uri.AbsoluteUri;
            // Save Thumbnail data
            if (!String.IsNullOrWhiteSpace(blob.DataUriThumbnail))
            {
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(newBlob.Id);
                await thumbStore.UploadTextAsync(blob.DataUriThumbnail);
                newBlob.ThumbnailUri = thumbStore.Uri.AbsoluteUri;
            }
            // Save metadata
            var metaTable = _tableClient.GetTableReference("blobmeta");
            var addOp = TableOperation.Insert(newBlob);
            _ = await metaTable.ExecuteAsync(addOp);
            return newBlob.Id;
        }

        /// <summary>
        /// Deletes a Blob from Azure Storage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string Id)
        {
            await EnsureContainers();
            // Load the Metadata
            var metaTable = _tableClient.GetTableReference("blobmeta");
            var tableEntity = MetaTable.GetKeysFromId(Id);
            var getOp = TableOperation.Retrieve<MetaTable>(tableEntity.PartitionKey, tableEntity.RowKey);
            var tableResult = await metaTable.ExecuteAsync(getOp);
            var metaData = (MetaTable)tableResult.Result;
            // Delete the blob
            var blobContainer = _blobClient.GetContainerReference("blobs");
            var blobStore = blobContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.BlobUri)).Name);
            _ = await blobStore.DeleteIfExistsAsync().ConfigureAwait(false);
            // Delete the Thumbnail
            if (metaData.ThumbnailUri != null)
            {
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.ThumbnailUri)).Name);
                _ = await thumbStore.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            // Delete the Metadata
            var deleteOp = TableOperation.Delete(metaData);
            _ = await metaTable.ExecuteAsync(deleteOp);
        }

        /// <summary>
        /// Gets a Blob from Azure Storage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<PEStorageBlob> GetBlobAsync(string Id)
        {
            await EnsureContainers();
            // Load the Metadata
            var metaTable = _tableClient.GetTableReference("blobmeta");
            var tableEntity = MetaTable.GetKeysFromId(Id);
            var getOp = TableOperation.Retrieve<MetaTable>(tableEntity.PartitionKey, tableEntity.RowKey);
            var tableResult = await metaTable.ExecuteAsync(getOp);
            var metaData = (MetaTable)tableResult.Result;
            // Load the Thumbnail
            string thumbData = null;
            if (metaData.ThumbnailUri != null)
            {
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.ThumbnailUri)).Name);
                using (var thumbStream = await thumbStore.OpenReadAsync())
                {
                    using (StreamReader reader = new StreamReader(thumbStream))
                    {
                        thumbData = await reader.ReadToEndAsync();
                    }
                }
            }
            // Return the Blob
            return new PEStorageBlob
            {
                ContentType = metaData.ContentType,
                DataUriThumbnail = thumbData,
                FileName = metaData.FileName
            };
        }

        /// <summary>
        /// Gets Blob Data from Azure Storage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<Stream> GetDataAync(string Id)
        {
            await EnsureContainers();
            // Load the Metadata
            var metaTable = _tableClient.GetTableReference("blobmeta");
            var tableEntity = MetaTable.GetKeysFromId(Id);
            var getOp = TableOperation.Retrieve<MetaTable>(tableEntity.PartitionKey, tableEntity.RowKey);
            var tableResult = await metaTable.ExecuteAsync(getOp);
            var metaData = (MetaTable)tableResult.Result;
            // Load the Data to MemoryStream
            var dataStream = new MemoryStream();
            var blobContainer = _blobClient.GetContainerReference("blobs");
            var blobStore = blobContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.BlobUri)).Name);
            using (var blobStream = await blobStore.OpenReadAsync())
            {
                await blobStream.CopyToAsync(dataStream);
            }
            // Rewind and Return the Stream
            dataStream.Seek(0, SeekOrigin.Begin);
            return dataStream;
        }

        /// <summary>
        /// Updates the Blob in Azure Storage
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string Id, PEStorageBlob blob, Stream data = null)
        {
            await EnsureContainers();
            // Load the Metadata
            var metaTable = _tableClient.GetTableReference("blobmeta");
            var tableEntity = MetaTable.GetKeysFromId(Id);
            var getOp = TableOperation.Retrieve<MetaTable>(tableEntity.PartitionKey, tableEntity.RowKey);
            var tableResult = await metaTable.ExecuteAsync(getOp);
            var metaData = (MetaTable)tableResult.Result;
            if (!String.IsNullOrWhiteSpace(metaData.ThumbnailUri) && String.IsNullOrWhiteSpace(blob.DataUriThumbnail))
            {
                // Delete the Thumbnail - it's been removed
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.ThumbnailUri)).Name);
                _ = await thumbStore.DeleteIfExistsAsync().ConfigureAwait(false);
                metaData.ThumbnailUri = null;
            }
            else if (String.IsNullOrWhiteSpace(metaData.ThumbnailUri) && !String.IsNullOrWhiteSpace(blob.DataUriThumbnail))
            {
                // Create a Thumbnail - one's been added
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(Id);
                await thumbStore.UploadTextAsync(blob.DataUriThumbnail);
                metaData.ThumbnailUri = thumbStore.Uri.AbsoluteUri;
            }
            else if (!String.IsNullOrWhiteSpace(blob.DataUriThumbnail))
            {
                // Update the Thumbnail
                var thumbsContainer = _blobClient.GetContainerReference("thumbs");
                var thumbStore = thumbsContainer.GetBlockBlobReference(Id);
                await thumbStore.UploadTextAsync(blob.DataUriThumbnail);
            }
            // Load the Data to Blob if Data's been updated
            if (data != null)
            {
                var blobContainer = _blobClient.GetContainerReference("blobs");
                var blobStore = blobContainer.GetBlockBlobReference(new CloudBlockBlob(new Uri(metaData.BlobUri)).Name);
                if (data.CanSeek && data.Position > 0)
                    data.Seek(0, SeekOrigin.Begin);
                await blobStore.UploadFromStreamAsync(data);
            }
            // Finally update the Metadata Table
            var replaceOp = TableOperation.Replace(metaData);
            _ = await metaTable.ExecuteAsync(replaceOp);
        }
    }
}
