using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage
{
    /// <summary>
    /// DTO for Blob Data (does not include actual file data)
    /// </summary>
    public class PEStorageBlob
    {
        /// <summary>
        /// Data-Uri Small Thumbnail (should be in web-compatible format e.g. PNG)
        /// </summary>
        public string DataUriThumbnail { get; set; }

        /// <summary>
        /// The name of the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// MimeType/ContentType
        /// </summary>
        public string ContentType { get; set; }
    }
}
