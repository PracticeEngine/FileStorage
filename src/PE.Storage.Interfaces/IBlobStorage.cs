﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE.Storage
{
    /// <summary>
    /// Abstraction to store persisted files for Practice Engine
    /// </summary>
    public interface IBlobStorage
    {
        /// <summary>
        /// Returns the Blob without the data for the given Id
        /// </summary>
        /// <returns>Blob</returns>
        Task<PEStorageBlob> GetBlobAsync(string Id);

        /// <summary>
        /// Returns the Data for a Blob
        /// </summary>
        /// <param name="Id">The Blob Id to return data</param>
        /// <returns>An open Stream that provides access to the data</returns>
        Task<Stream> GetDataAync(string Id);

        /// <summary>
        /// Creates a new blob in the system
        /// </summary>
        /// <returns>New blob Id</returns>
        Task<string> CreateAsync(PEStorageBlob blob);

        /// <summary>
        /// Updates the provided blob
        /// </summary>
        /// <param name="blob">The Blob to Update</param>
        /// <param name="data">Optional readable stream to retrieve new data</param>
        /// <returns>Nothing</returns>
        Task UpdateAsync(PEStorageBlob blob, Stream data = null);

        /// <summary>
        /// Deletes the provided blob
        /// </summary>
        /// <param name="Id">The Id of the blob that should be removed</param>
        /// <returns>Nothing</returns>
        Task DeleteAsync(string Id);
    }
}
