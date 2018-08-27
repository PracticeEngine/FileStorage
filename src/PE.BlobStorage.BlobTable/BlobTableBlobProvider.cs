using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;

namespace PE.BlobStorage.BlobTable
{
    public class BlobTableBlobProvider : IBlobStorage
    {
        private readonly BlobTableOptions _options;

        /// <summary>
        /// Constructor that uses provided settings
        /// </summary>
        /// <param name="options"></param>
        public BlobTableBlobProvider(BlobTableOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _options = options;
        }

        /// <summary>
        /// Creates a new Blob in tblBlobStorage
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> CreateAsync(PEStorageBlob blob, Stream data)
        {
            if (data.CanSeek && data.Position > 0)
                data.Seek(0, SeekOrigin.Begin);
            int newBlobId;
            using (var con = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO [dbo].[tblBlobStorage] (Thumbnail,Blob,FileName,ContentType) VALUES (@Thumbnail,@Blob,@FileName,@ContentType); SELECT CAST(SCOPE_IDENTITY() as int);";
                    cmd.Parameters.AddWithValue("@Thumbnail", blob.DataUriThumbnail ?? "");
                    cmd.Parameters.AddWithValue("@Blob", new SqlBytes(data));
                    cmd.Parameters.AddWithValue("@FileName", blob.FileName);
                    cmd.Parameters.AddWithValue("@ContentType", blob.ContentType);
                    await con.OpenAsync().ConfigureAwait(false);
                    newBlobId = (int) await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }
            return newBlobId.ToString("D9");
        }

        /// <summary>
        /// Deletes a Blob in tblBlobStorage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string Id)
        {
            int blobId = int.Parse(Id);
            using (var con = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE [dbo].[tblBlobStorage] WHERE [Id] = @BlobId";
                    cmd.Parameters.AddWithValue("@BlobId", blobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets a Blob from tblBlobStorage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<PEStorageBlob> GetBlobAsync(string Id)
        {
            int blobId = int.Parse(Id);
            PEStorageBlob blob = new PEStorageBlob();
            using (var con = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SELECT [Thumbnail], [FileName], [ContentType] FROM [dbo].[tblBlobStorage] WHERE [Id] = @BlobId";
                    cmd.Parameters.AddWithValue("@BlobId", blobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                    {
                        if (await reader.ReadAsync())
                        {
                            blob.DataUriThumbnail = reader.IsDBNull(0) ? null : await reader.GetFieldValueAsync<string>(0).ConfigureAwait(false); // Use Async as this field could be long
                            blob.FileName = reader.GetString(1);
                            blob.ContentType = reader.GetString(2);
                        }
                    }
                }
            }
            return blob;
        }

        /// <summary>
        /// Gets Data for a Blob from tblBlobStorage
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<Stream> GetDataAync(string Id)
        {
            int blobId = int.Parse(Id);
            var ms = new MemoryStream();
            using (var con = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SELECT [Blob] FROM [dbo].[tblBlobStorage] WHERE [Id] = @BlobId";
                    cmd.Parameters.AddWithValue("@BlobId", blobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync())
                        {
                            using (var sqlStream = reader.GetStream(0))
                            {
                                await sqlStream.CopyToAsync(ms).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            // Rewind and Return the Stream
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// Updates Data for a Blob in tblBlobStorage
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="blob"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string Id, PEStorageBlob blob, Stream data = null)
        {
            int blobId = int.Parse(Id);
            using (var con = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    if (data != null)
                    {
                        cmd.CommandText = "UPDATE [dbo].[tblBlobStorage] SET [Thumbnail]=@Thumbnail,[Blob]=@Blob,[FileName]=@FileName,[ContentType]=@ContentType WHERE [Id] = @BlobId";
                        cmd.Parameters.AddWithValue("@Blob", new SqlBytes(data));
                    }
                    else
                    {
                        cmd.CommandText = "UPDATE [dbo].[tblBlobStorage] SET [Thumbnail]=@Thumbnail,[FileName]=@FileName,[ContentType]=@ContentType WHERE [Id] = @BlobId";
                    }
                    cmd.Parameters.AddWithValue("@Thumbnail", blob.DataUriThumbnail);
                    cmd.Parameters.AddWithValue("@FileName", blob.FileName);
                    cmd.Parameters.AddWithValue("@ContentType", blob.ContentType);
                    cmd.Parameters.AddWithValue("@BlobId", blobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
