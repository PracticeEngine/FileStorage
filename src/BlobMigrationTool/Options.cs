using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobMigrationTool
{
    public class Options
    {
        [Option("database", Required =true, HelpText ="Database Connection String")]
        public string DatabaseConnectionString { get; set; }

        [Option("from", Required = true, HelpText = "Provider to Migrate from")]
        public string ProviderFrom { get; set; }

        [Option("to", Required = true, HelpText = "Provider to Migrate to")]
        public string ProviderTo { get; set; }
    }
}
