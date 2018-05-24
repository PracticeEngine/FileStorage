using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.BlobStorage.AzureStorage
{
    /// <summary>
    /// Stores Metadata in table storage (PartitionKey = YYYYMMDD, RowKey = GUID)
    /// ID Format = Partitionkey-RowKey
    /// </summary>
    public class MetaTable : TableEntity
    {

        /// <summary>
        /// Constructs a New blob from a PEStorageBlob
        /// Does not include Thumbnail or Blob Uri values
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static MetaTable NewEntity(PEStorageBlob blob)
        {
            return new MetaTable()
            {
                PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
                RowKey = Guid.NewGuid().ToString("N"),
                FileName = blob.FileName,
                ContentType = blob.ContentType
            };
        }

        /// <summary>
        /// Constructs a TableEntity with PartitionKey and RowKey from a Blob Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static TableEntity GetKeysFromId(string Id)
        {
            var parts = Id.Split('/');
            return new TableEntity()
            {
                PartitionKey = parts[0],
                RowKey = parts[1]
            };
        }

        /// <summary>
        /// Blob Storage Id
        /// </summary>
        [IgnoreProperty]
        public string Id
        {
            get
            {
                return this.PartitionKey + "/" + this.RowKey;
            }
        }

        /// <summary>
        /// The name of the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// MimeType/ContentType
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Storage Uri to thumbnail
        /// </summary>
        public string ThumbnailUri { get; set; }

        /// <summary>
        /// Storage Uri to Blob
        /// </summary>
        public string BlobUri { get; set; }
    }
}
