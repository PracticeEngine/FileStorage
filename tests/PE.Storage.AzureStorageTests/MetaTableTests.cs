using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PE.Storage.AzureStorage;

namespace PE.Storage.AzureStorageTests
{
    [TestClass]
    public class MetaTableTests
    {
        [TestMethod]
        public void NewEntity_CreatesPartitions_And_RowKey_Tests()
        {
            // Setup
            var blob = new PEStorageBlob
            {
                ContentType = "application/test",
                FileName = "unit.test",
                DataUriThumbnail = "dataBlob"
            };
            var entity = MetaTable.NewEntity(blob);

            // Test
            var key = entity.PartitionKey;
            Assert.AreEqual(8, key.Length);
            Assert.AreEqual(DateTime.UtcNow.Year, Int32.Parse(key.Substring(0, 4)));
            Assert.AreEqual(DateTime.UtcNow.Month, Int32.Parse(key.Substring(4, 2)));
            Assert.AreEqual(DateTime.UtcNow.Day, Int32.Parse(key.Substring(6, 2)));
            Assert.AreEqual("application/test", entity.ContentType);
            Assert.AreEqual("unit.test", entity.FileName);
            Assert.AreEqual(32, entity.RowKey.Length);
        }

        [TestMethod]
        public void NewEntity_Uniqueness_Test()
        {
            // Setup
            var blob = new PEStorageBlob
            {
                ContentType = "application/test",
                FileName = "unit.test",
                DataUriThumbnail = "dataBlob"
            };
            var entity1 = MetaTable.NewEntity(blob);
            var entity2 = MetaTable.NewEntity(blob);

            // Test
            Assert.AreEqual(entity1.PartitionKey, entity2.PartitionKey);
            Assert.AreNotEqual(entity1.RowKey, entity2.RowKey);
        }

        [TestMethod]
        public void NewEntity_Can_Inflate_FromGetKeysFromId()
        {
            // Setup
            var blob = new PEStorageBlob
            {
                ContentType = "application/test",
                FileName = "unit.test",
                DataUriThumbnail = "dataBlob"
            };
            var entity1 = MetaTable.NewEntity(blob);
            var entity2 = MetaTable.GetKeysFromId(entity1.Id);

            // Test
            Assert.AreEqual(entity1.PartitionKey, entity2.PartitionKey);
            Assert.AreEqual(entity1.RowKey, entity2.RowKey);
        }
    }
}
