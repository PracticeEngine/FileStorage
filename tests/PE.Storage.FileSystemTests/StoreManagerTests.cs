using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PE.Storage.FileSystem;

namespace PE.Storage.FileSystemTests
{
    [TestClass]
    public class StoreManagerTests
    {
        [TestMethod]
        public void StoreManager_Ensures_Directory()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp).TrimEnd('\\')+ "\\storetest";
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            // Test
            _ = new StoreManager(path);

            // Verify
            Assert.IsTrue(Directory.Exists(path));
        }

        [TestMethod]
        public async Task StoreManager_Ensures_Randomness()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp).TrimEnd('\\') + "\\storeloadtest";
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            // Test
            var mgr = new StoreManager(path);
            var results = new List<string>();
            for(var x =0;x<5000;x++)
            {
                var file = await mgr.GetStoreDb().NextAvailableFile();
                results.Add(file);
            }

            // Verify
            var distinctStores = results.Select(f => f.Substring(0, f.IndexOf('\\'))).Distinct().Count();
            Assert.IsTrue(distinctStores > 450, "More than 450 stores created after 5,000 files");
        }

        [TestMethod]
        public async Task StoreManager_Gets_NextValue_Same_Store()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp).TrimEnd('\\') + "\\samestoretest";
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            // Test
            var mgr = new StoreManager(path);
            var file = await mgr.GetStoreDb().NextAvailableFile();
            var store = file.Substring(0, file.IndexOf('\\'));
            var nextFile = await mgr.GetStoreDb(store).NextAvailableFile();

            // Verify
            Assert.AreEqual(file.Substring(0, file.IndexOf('\\')), nextFile.Substring(0, file.IndexOf('\\')));
            Assert.AreEqual("0001", file.Substring(file.LastIndexOf('\\') + 1));
            Assert.AreEqual("0002", nextFile.Substring(file.LastIndexOf('\\') + 1));
        }

    }
}
