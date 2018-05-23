using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobMigrationTool
{
    public class BlobDetails
    {
        public int BlobId { get; set; }

        public string ProviderName { get; set; }

        public string ProviderId { get; set; }

        public DateTimeOffset? Expiry { get; set; }
    }
}
