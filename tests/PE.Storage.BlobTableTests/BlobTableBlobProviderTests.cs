using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PE.Storage.BlobTable;

namespace PE.Storage.BlobTableTests
{
    [TestClass]
    public class BlobTableBlobProviderTests
    {
        [TestCategory("Integration")]
        [TestMethod]
        public async Task BlobTableProvider_IntegrationTest()
        {
            // Setup
            var blob1 = new PEStorageBlob
            {
                ContentType = "application/unit-test",
                FileName = "afile.unittest",
                DataUriThumbnail = "unit.thumb"
            };
            var provider = new BlobTableBlobProvider(new BlobTableOptions { DatabaseConnectionString = "Data Source=.;Initial Catalog=Engine_Master_961;Integrated Security=True" });

            // Tests
            PEStorageBlob blob2;
            byte[] array1;
            byte[] array2;
            string id1;
            using (var ms1 = new MemoryStream(new byte[] { 0x002, 0x003, 0x034, 0xac }))
            {
                // Save it
                id1 = await provider.CreateAsync(blob1, ms1);
                // Now Retrieve it
                blob2 = await provider.GetBlobAsync(id1);
                using (var ms2 = await provider.GetDataAync(id1))
                {

                    // Verify Data
                    ms1.Seek(0, SeekOrigin.Begin);
                    array1 = ms1.ToArray();
                    ms2.Seek(0, SeekOrigin.Begin);
                    var resultStream = new MemoryStream();
                    ms2.CopyTo(resultStream);
                    array2 = resultStream.ToArray();
                }
            }
            // Delete
            await provider.DeleteAsync(id1);


            // Verify
            Assert.AreEqual(blob1.ContentType, blob2.ContentType);
            Assert.AreEqual(blob1.FileName, blob2.FileName);
            Assert.AreEqual(blob1.DataUriThumbnail, blob2.DataUriThumbnail);
            CollectionAssert.AreEqual(array1, array2, "Data is not the same");
        }
    }
}
