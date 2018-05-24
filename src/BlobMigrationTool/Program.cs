using CommandLine;
using CommandLine.Text;
using PE.BlobStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobMigrationTool
{
    class Program
    {
        static Type[] providerTypes = new[] {
            typeof(PE.BlobStorage.AzureStorage.AzureStorageBlobProvider),
            typeof(PE.BlobStorage.BlobTable.BlobTableBlobProvider),
            typeof(PE.BlobStorage.FileSystem.FileSystemBlobProvider)
        };

        /// <summary>
        /// The Entry Point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("PE Blob Migration Tool");
            Console.WriteLine();
            Console.WriteLine("Available Providers are:");
            foreach(var type in providerTypes)
            {
                Console.Write("\t");
                Console.WriteLine(type.FullName);
            }
            Console.WriteLine("");
            Console.WriteLine("IMPORTANT: Use the .config to provide all configuration values to the provider!");
            Console.WriteLine("");
            Console.WriteLine();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Runs the Migration Process based on the options Provided
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        static int RunOptionsAndReturnExitCode(Options options)
        {
            try
            {
                RunMigration(options).Wait();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("All Blobs Moved Successfully!");
                return 0;
            }
            catch (AggregateException aex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var ex in aex.InnerExceptions)
                {
                    Console.WriteLine(ex.Message);
                }
                return 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Handles any Parsing Errors
        /// </summary>
        /// <param name="errs"></param>
        static void HandleParseError(IEnumerable<Error> errs)
        {

        }

        /// <summary>
        /// Fund the Migration based on the Options Provided
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        static async Task RunMigration(Options options)
        {
            var blobManager = new BlobManager(options.DatabaseConnectionString);
            await blobManager.VerifyConnection();
            IBlobStorage fromProvider = GetProvider(options.ProviderFrom);
            IBlobStorage toProvider = GetProvider(options.ProviderTo);
            var blobsToMigrate = await blobManager.ListAllBlobsForProvider(options.ProviderFrom);
            foreach(var blob in blobsToMigrate)
            {
                Console.Write(blob.ProviderId);
                Console.Write(" => ");
                if (blob.Expiry < DateTime.UtcNow)
                {
                    await blobManager.DeleteBlob(blob);
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Expired");
                    Console.ForegroundColor = color;
                }
                else
                {
                    var sourceId = blob.ProviderId;
                    var blobData = await fromProvider.GetBlobAsync(sourceId);
                    using (var blobStream = await fromProvider.GetDataAync(blob.ProviderId))
                    {
                        blob.ProviderName = options.ProviderTo;
                        blob.ProviderId = await toProvider.CreateAsync(blobData, blobStream);
                        await blobManager.UpdateBlobLocation(blob);
                        await fromProvider.DeleteAsync(sourceId);
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(blob.ProviderId);
                        Console.ForegroundColor = color;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the Provider based on the Name
        /// </summary>
        /// <param name="ProviderName"></param>
        /// <returns></returns>
        static IBlobStorage GetProvider(string ProviderName)
        {
            var type = providerTypes.Where(t => t.FullName == ProviderName).FirstOrDefault();
            if (type == null)
                throw new Exception($"{ProviderName} is not a valid provider type.");
            return (IBlobStorage)Activator.CreateInstance(type);
        }
    }
}
