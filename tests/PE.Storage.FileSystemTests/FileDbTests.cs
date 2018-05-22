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
    public class FileDbTests
    {
        [TestMethod]
        public void ThrowsIfDirectoryDoesNotExist()
        {
            Assert.ThrowsException<DirectoryNotFoundException>(() =>
            {
                new FileDb("a:\\no_dir\\");
            }, "Constructor did not throw");
        }

        [TestMethod]
        public async Task NextAvailableFile_Creates_LockFile()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp);
            var store = new DirectoryInfo(path).Name;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var lockFile = Path.Combine(path, "store.lock");
            if (File.Exists(lockFile))
                File.Delete(lockFile);

            // Test
            var db = new FileDb(path);
            var file = await db.NextAvailableFile();

            // Verify
            Assert.IsTrue(File.Exists(lockFile));
            Assert.AreEqual(store+"\\0001\\0001\\0001", file);
        }


        [TestMethod]
        public async Task NextAvailableFile_Increases_On_Disk()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp) + "\\nextfile";
            var store = new DirectoryInfo(path).Name;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var lockFile = Path.Combine(path, "store.lock");
            if (File.Exists(lockFile))
                File.Delete(lockFile);

            // Test
            var db = new FileDb(path);
            string file = "";
            for (var x = 1; x <= 318; x++)
            {
                file = await db.NextAvailableFile();
            }

            // Verify
            Assert.IsTrue(File.Exists(lockFile));
            Assert.AreEqual(store + "\\0001\\0001\\013E", file);
        }

        [TestMethod]
        public void NextAvailableFile_Deadlock_Retry_Test()
        {
            // Setup
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            var path = Path.GetDirectoryName(tmp) + "\\deadlocktest";
            var store = new DirectoryInfo(path).Name;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var lockFile = Path.Combine(path, "store.lock");
            if (File.Exists(lockFile))
                File.Delete(lockFile);

            // Test - intentionally run all at once to create deadlocks on the file
            var db = new FileDb(path);
            var taskList = new List<Task<string>>();
            for (var x = 1; x <= 5; x++)
            {
                taskList.Add(db.NextAvailableFile());
            }
            Task.WaitAll(taskList.ToArray());

            // Verify
            var distincts = taskList.Select(task => task.Result).Distinct();
            var distinctCount = distincts.Count();
            Assert.AreEqual(5, distinctCount);
        }

        [TestMethod]
        public void CalculateNextFileValue_Increases_UpperFolder_After500Thousand()
        {
            // Setup
            var mockStructure = new int[] { 1, 1, 0 };

            // Test
            for (var x = 1; x <= 500001; x++)
            {
                FileDb.CalculateNextFileValue(mockStructure);
            }

            // Verify
            Assert.AreEqual(2, mockStructure[0]);
            Assert.AreEqual(1, mockStructure[1]);
            Assert.AreEqual(1, mockStructure[2]);
        }

        [TestMethod]
        public void CalculateNextFileValue_Upper_Threshold_Test()
        {
            // Setup
            var mockStructure = new int[] { 1, 1, 0 };

            // Test adding 500 million files to a single store
            for (var x = 0; x < 500000000; x++)
            {
                FileDb.CalculateNextFileValue(mockStructure);
            }

            // Verify
            Assert.AreEqual(1000, mockStructure[0]);
            Assert.AreEqual(1000, mockStructure[1]);
            Assert.AreEqual(500, mockStructure[2]);
        }
    }
}
