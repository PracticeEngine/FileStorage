using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobMigrationTool
{
    public class BlobManager
    {
        private readonly string _conString;
        public BlobManager(string ConnectionString)
        {
            _conString = ConnectionString;
        }

        /// <summary>
        /// Verifies the Database Connection Works
        /// </summary>
        /// <returns></returns>
        public async Task<bool> VerifyConnection()
        {
            try
            {
                using (var con = new SqlConnection(_conString))
                {
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "SELECT TOP 1 1 FROM [tblBlob]";
                        await con.OpenAsync().ConfigureAwait(false);
                        await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    }
                }
                return true;
            }
            catch(SqlException se)
            {
                Console.WriteLine(se.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Returns list of all blobs in the database for a provider
        /// </summary>
        /// <param name="ProviderName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BlobDetails>> ListAllBlobsForProvider(string ProviderName)
        {
            List<BlobDetails> results = new List<BlobDetails>();
            using (var con = new SqlConnection(_conString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SELECT [Id], [ProviderName], [ProviderId], [Expiry] FROM [tblBlob] WHERE [ProviderName] = @ProviderName";
                    cmd.Parameters.AddWithValue("@ProviderName", ProviderName);
                    await con.OpenAsync().ConfigureAwait(false);
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var nextBlob = new BlobDetails
                            {
                                BlobId = reader.GetInt32(0),
                                ProviderName = reader.GetString(1),
                                ProviderId = reader.GetString(2),
                                Expiry = reader.IsDBNull(3) ? (DateTimeOffset?)null : reader.GetDateTimeOffset(3)
                            };
                            results.Add(nextBlob);
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Update the database with new blob details
        /// </summary>
        /// <param name="blobDetails">updated Details</param>
        /// <returns></returns>
        public async Task UpdateBlobLocation(BlobDetails blobDetails)
        {
            using (var con = new SqlConnection(_conString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "UPDATE [tblBlob] SET [ProviderName] = @ProviderName, [ProviderId] = @ProviderId WHERE [Id] = @BlobId";
                    cmd.Parameters.AddWithValue("@ProviderName", blobDetails.ProviderName);
                    cmd.Parameters.AddWithValue("@ProviderId", blobDetails.ProviderId);
                    cmd.Parameters.AddWithValue("@BlobId", blobDetails.BlobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Removes Blob Details from the Database
        /// </summary>
        /// <param name="blobDetails"></param>
        /// <returns></returns>
        public async Task DeleteBlob(BlobDetails blobDetails)
        {
            using (var con = new SqlConnection(_conString))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM [tblBlob] WHERE [Id] = @BlobId";
                    cmd.Parameters.AddWithValue("@BlobId", blobDetails.BlobId);
                    await con.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
